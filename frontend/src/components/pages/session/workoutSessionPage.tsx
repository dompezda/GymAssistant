import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ExercisePickerModal from "../../exercises/ExercisePickerModal";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";
import { RefreshCw, X } from "lucide-react";
import WorkoutSessionExerciseDetails from "./workoutSessionExerciseDetailsPage";
import "./workoutSessionPage.css";

type WorkoutSession = {
  id: string;
  workoutTemplateId: string | null;
  exercises: WorkoutSessionExercise[];
};

type WorkoutSessionExercise = {
  id: string; // WorkoutSessionExerciseId - TO idzie do dirtyIds
  exerciseId: string; // ExerciseId ze słownika
  exerciseName: string;
  order: number;
  sets: WorkoutSessionSet[];
};

type WorkoutSessionSet = {
  id: string; // WorkoutSessionSetId
  setNumber: number;
  weight: number | null;
  reps: number | null;
};

type ExerciseModalMode = "closed" | "picker" | "create";

export default function WorkoutSessionPage() {
  //const navigate = useNavigate();
  const { id } = useParams();
  const [session, setSession] = useState<WorkoutSession | null>(null);
  const [dirtyIds, setDirtyIds] = useState<string[]>([]); 
  const navigate = useNavigate();
  const exercises = session?.exercises ?? [];
  const [exerciseModalMode, setExerciseModalMode] = useState<ExerciseModalMode>("closed");

  const isSavingRef = useRef<boolean>(false);
  const [isSaving,setIsSaving] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [selectedExerciseId, setSelectedExerciseId] =
      useState<string | null>(null);

      const selectedExercise = session?.exercises.find(
      x => x.id === selectedExerciseId
  ) ?? null;
useEffect(() => {
  loadSession();
}, [id]);


async function loadSession(): Promise<boolean> {
  if(!id) return false;
  try {
    const res = await fetchApi(`/api/workout-sessions/${id}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        }
      })
      if(!res.ok)
      {
        setErrorMessage("Błąd przy odczycie sesji z serwera. Spróbuj ponownie.")
        return false;
      }
      const data = await res.json();
      setSession(data);
  } catch(error) {
    console.error("Błąd pobieranie sesji",error);
    setErrorMessage("Błąd połączenia z serwerem. Spróbuj ponownie.");
    return false;
  }
  return true;
}

async function refreshSession(): Promise<void> {
  if(!id) return;
  if(isSavingRef.current == true) return;
  if(dirtyIds.length>0)
  {
    const confirm = window.confirm("W sesji istnieją niezapisane w bazie dane. Przy odświeżeniu sesji zostaną utracone.")
    if(!confirm) return;
  }
  setErrorMessage(null);
  setIsSaving(true);
  isSavingRef.current=true;
  try {
    const res = await loadSession();
    if(!res)
    {
      return;
    }
    setDirtyIds([]);
  }
  finally
  {
      setIsSaving(false);
      isSavingRef.current=false;
  }
}

if (!session) {
  return (
    <section className="page">
      {errorMessage ? (
        <>
          <p>{errorMessage}</p>
          <button onClick={refreshSession} disabled={isSaving}>
            Spróbuj ponownie
          </button>
        </>
      ) : (
        <p>Ładowanie sesji...</p>
      )}
    </section>
  );
}
  async function saveCurrentExerciseIfDirty(exercise: WorkoutSessionExercise): Promise<boolean>
  {
    if(!dirtyIds.includes(exercise.id))
      return true;
    const res = await patchExerciseData(exercise)  
    if(res.ok)
    {
      setDirtyIds(prev => prev.filter((elem) => exercise.id !==elem)); 
      return true; 
    }
    return false;
  }
  
  async function patchExerciseData (sessionExercise: WorkoutSessionExercise) : Promise<Response> {
    return fetchApi(`/api/workout-sessions/${id}/exercises/${sessionExercise.id}`, {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        sets: sessionExercise.sets
      }),
    })
  }

  async function finishSession() {

    if (isSavingRef.current) return;
    setErrorMessage(null);
    isSavingRef.current = true;
    setIsSaving(true);
    
    const dirtyExercises = exercises.filter(x => dirtyIds.includes(x.id));
    try
    {
        const responses = await Promise.allSettled(
        dirtyExercises.map(exercise => patchExerciseData(exercise))
      );

      const savedIds = dirtyExercises
        .filter((exercise, index) => responses[index].status == "fulfilled" && responses[index].value.ok)
        .map(exercise => exercise.id);

      setDirtyIds(prev =>
        prev.filter(id => !savedIds.includes(id))
      );
      if (!responses.every(response => response.status == "fulfilled" && response.value.ok)) {
        setErrorMessage("Nie udało się zapisać wszystkich ćwiczeń.");
        return;
      }

      const finishResponse = await fetchApi(`/api/workout-sessions/${id}/finish`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json"
        }
      });

      if (!finishResponse.ok) {
        setErrorMessage("Nie udało się zakończyć sesji");
        return;
      }
      navigate(`/workout-sessions/${id}/summary`);
    }
      catch (error) {
      console.error("Finish session failed:", error);
      setErrorMessage("Wystąpił nieoczekiwany błąd. Spróbuj ponownie.");
    }
    finally
    {
      setIsSaving(false);
      isSavingRef.current=false;
    }
}

  function markExerciseDirty(sessionExerciseId: string) {
  setDirtyIds(prev =>
    prev.includes(sessionExerciseId)
      ? prev
      : [...prev, sessionExerciseId]
  );
}

  async function addExerciseToCurrentSession(exerciseId:string) {
    if(isSavingRef.current==true) return;
    if (!session) return;

    const existingIndex = session.exercises.findIndex(
      x => x.exerciseId === exerciseId
    );
    if (existingIndex !== -1) {
      return;
    }
    setErrorMessage(null);
    isSavingRef.current=true;
    setIsSaving(true);
    try
    {
    const response = await fetchApi(`/api/workout-sessions/${session.id}/exercises`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        exerciseId: exerciseId
      })
    });
    if (!response.ok) {
      setErrorMessage("Nie udało się dodać ćwiczenia.")
      const error = await response.text();
      console.error(`Nie udało się dodać ćwiczenia. ${error}`);
      return;
    }
    const data = await response.json();
    const newSessionExercise: WorkoutSessionExercise = {
        id: data.id,
        exerciseId: data.exerciseId,
        exerciseName: data.exerciseName,
        order: data.order,
        sets: data.sets
      };
      setSession((prev) => {
        if (!prev) return prev;

        return {
          ...prev,
          exercises: [...prev.exercises, newSessionExercise],
        };
      });
    }
    catch (error) {
      console.error("Błąd podczas dodawania ćwiczenia:", error);
      setErrorMessage("Wystąpił nieoczekiwany błąd. Spróbuj ponownie.");
    }
    finally
    {
      isSavingRef.current=false;
      setIsSaving(false);
      setSelectedExerciseId(null);
    }
  }

  async function deleteExercise(id: string): Promise<void> {
    if(isSavingRef.current == true) return;
    if(!session) return;
    if(!id) return;
    const exercise = session.exercises.find(x => id == x.id);
    if (!exercise)
        return;
    const confirmed = window.confirm(
    `Czy na pewno chcesz usunąć ćwiczenie "${exercise.exerciseName}" z tej sesji?`
      );
    if (!confirmed) {
      return;
    }
    setErrorMessage(null);
    isSavingRef.current = true;
    setIsSaving(true);
    try {
    const response = await fetchApi(`/api/workout-sessions/${session.id}/exercises/${id}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json"
      }
    });

    if(!response.ok)
    {
      setErrorMessage("Nie udało się usunąć ćwiczenia. Spróbuj ponownie.");
      console.error("Błąd przy usuwaniu:", response.status);
      return;
    }
    setDirtyIds(prev => prev.filter(exerciseId => exerciseId !== id));
    const newExercises = session.exercises.filter((e)=>e.id!==id);
    setSession((prev)=>{
      if(!prev) 
        return prev;

        return {
          ...prev,
          exercises: newExercises
        };
      });
    } catch (error) {
        console.error(error);
        setErrorMessage("Wystąpił nieoczekiwany błąd. Spróbuj ponownie.");
    } finally {
        isSavingRef.current = false;
        setIsSaving(false);
    }
    
  }

  async function handleCloseExercise(): Promise<void> {
    if (isSavingRef.current || !selectedExercise) return;

    setErrorMessage(null);
    isSavingRef.current = true;
    setIsSaving(true);

    try {
        const saved = await saveCurrentExerciseIfDirty(selectedExercise);

        if (!saved) {
            setErrorMessage("Nie udało się zapisać ćwiczenia.");
            return;
        }

        setSelectedExerciseId(null);
    } catch (error) {
        console.error("Błąd zapisu ćwiczenia:", error);
        setErrorMessage("Wystąpił błąd podczas zapisywania ćwiczenia.");
    } finally {
        isSavingRef.current = false;
        setIsSaving(false);
    }
}
  
return (
  <section className="page workout-session-page">
    <div className="page-header workout-session-topbar">
  <button
    type="button"
    className="btn back-button"
    disabled={isSaving}
    onClick={() => {
      if (selectedExercise) {
        void handleCloseExercise();
      } else {
        navigate("/");
      }
    }}
  >
    ← Wróć
  </button>

  {!selectedExercise && (
    <button
      type="button"
      className="btn-refresh"
      aria-label="Odśwież dane sesji"
      title="Odśwież dane sesji"
      onClick={refreshSession}
      disabled={isSaving}
    >
      <RefreshCw size={18} />
    </button>
  )}
</div>
    {selectedExercise ? (
      <WorkoutSessionExerciseDetails
        isOpen={selectedExercise !== null}
        isSaving={isSaving}
        exercise={selectedExercise}
        setSession={setSession}
        onExerciseChanged={markExerciseDirty}
      />
    ) : (
      <>
        
        <div className="workout-session-content">
          <button
            type="button"
            className="btn btn-secondary"
            disabled={isSaving}
            onClick={() => setExerciseModalMode("picker")}
          >
            + Dodaj ćwiczenie
          </button>

          {exercises.length === 0 ? (
            <p>Brak ćwiczeń w tej sesji.</p>
          ) : (
            exercises.map((exercise) => (
              <article
                key={exercise.id}
                className="exercise-card"
                onClick={() => {
                  if (!isSaving) {
                    setSelectedExerciseId(exercise.id);
                  }
                }}
              >
                <div className="exercise-name">
                  {exercise.exerciseName}
                </div>

                <div className="session-exercise-meta">
                  <span>{exercise.sets.length} serie</span>

                  <button
  type="button"
  className="btn-remove-exercise"
  aria-label={`Usuń ${exercise.exerciseName}`}
  disabled={isSaving || exercises.length <= 1}
  onClick={(e) => {
    e.stopPropagation();
    void deleteExercise(exercise.id);
  }}
>
  <X size={18} />
</button>
                </div>
              </article>
            ))
          )}
        </div>
      </>
    )}

    {errorMessage && (
      <p className="form-error" role="alert">
        {errorMessage}
      </p>
    )}

    {!selectedExercise && (
      <div className="workout-session-actions">
        <div className="template-form-actions">
          <button
            type="button"
            disabled={isSaving}
            className="btn btn-primary finish-workout-button"
            onClick={() => void finishSession()}
          >
            Zakończ trening
          </button>
        </div>
      </div>
    )}

    <ExercisePickerModal
      isOpen={exerciseModalMode === "picker"}
      onClose={() => setExerciseModalMode("closed")}
      onCreateNew={() => setExerciseModalMode("create")}
      onSelect={(exercise) => {
        void addExerciseToCurrentSession(exercise.id);
      }}
    />

    <CreateExerciseModal
      isOpen={exerciseModalMode === "create"}
      onClose={() => setExerciseModalMode("closed")}
      onCreated={(exercise) => {
        void addExerciseToCurrentSession(exercise.id);
        setExerciseModalMode("closed");
      }}
    />
  </section>
);
}
