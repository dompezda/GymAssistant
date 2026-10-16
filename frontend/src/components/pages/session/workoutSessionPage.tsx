import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ExercisePickerModal from "../../exercises/ExercisePickerModal";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";
import { RefreshCw } from "lucide-react";

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
  const [currentExerciseIndex, setCurrentExerciseIndex] = useState(0);
  const currentExercise = exercises[currentExerciseIndex];
  const [exerciseModalMode, setExerciseModalMode] = useState<ExerciseModalMode>("closed");
  const [isExerciseMenuOpen,setIsExerciseMenuOpen] = useState<boolean>(false);
  const touchStartX = useRef<number | null>(null);
  const touchEndX = useRef<number | null>(null);
  const touchStartY = useRef<number | null>(null);
  const touchEndY = useRef<number | null>(null);
  const isSavingRef = useRef<boolean>(false);
  const [isSaving,setIsSaving] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

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
      setCurrentExerciseIndex(0);
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

if (exercises.length === 0 || !currentExercise) {
  return (
    <section className="page">
      <p>Brak ćwiczeń w tej sesji.</p>
      <button onClick={refreshSession} disabled={isSaving}>
        Odśwież sesję
      </button>
      <button onClick={() => navigate("/")}>
        ← Wróć
      </button>
    </section>
  );
}

  function updateSetValue(
  setId: string,
  field: "weight" | "reps",
  value: number | null
) {
  if (isSavingRef.current) return;
  if (!currentExercise) return;
  setDirtyIds(prev =>
    prev.includes(currentExercise.id)
      ? prev
      : [...prev, currentExercise.id]
  );
    setSession(prev => {
    if (!prev) return prev;
    return {
      ...prev,
      exercises: prev.exercises.map(exercise => {
        if (exercise.id !== currentExercise.id) {
          return exercise;
        }
        return {
          ...exercise,
          sets: exercise.sets.map(set => {
            if (set.id !== setId) {
              return set;
            }
            return {
              ...set,
              [field]: value
            };
          })
        };
      })
    };
  });
  }

  async function goToExercise(index: number): Promise<void> {

    if (isSavingRef.current == true) return;
    if (index < 0 || index >= exercises.length) return;
    setErrorMessage(null);
    setIsExerciseMenuOpen(false)
    isSavingRef.current = true;
    setIsSaving(true);
    try {
      const res = await saveCurrentExerciseIfDirty();
      if(res)
        setCurrentExerciseIndex(index);
      else
      {
        setErrorMessage("Brak połączenia z serwerem. Sprawdź połączenie i spróbuj ponownie.");
      }
    } catch {
      setErrorMessage("Nie udało się zapisać ćwiczenia. Spróbuj ponownie.");
    }
    finally
    {
      setIsSaving(false);
      isSavingRef.current = false;
    }
  }

  async function saveCurrentExerciseIfDirty(): Promise<boolean>
  {
    if(!dirtyIds.includes(currentExercise.id))
      return true;
    const res = await patchExerciseData(currentExercise)  
    if(res.ok)
    {
      setDirtyIds(prev => prev.filter((elem) => currentExercise.id !==elem)); 
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

  function addSet(): void {
    if(isSavingRef.current==true) return;
    markExerciseDirty(currentExercise.id);
    setSession((prev)=> 
    {
      if(!prev)
        return prev;

    return {
    ...prev,
    exercises: prev.exercises.map((exercise) =>
    {
      if(exercise.id !== currentExercise.id)
        return exercise;

        const newSet : WorkoutSessionSet ={
          id: crypto.randomUUID(),
          setNumber: exercise.sets.length+1,
          weight: null,  
          reps : null,           
        }
        //markExerciseDirty(currentExercise.id);

      return {
        ...exercise,
        sets:
        [
          ...exercise.sets,
          newSet
        ]
      }
    })}
    
  })
  }

  function deleteSet(setId: string): void {
    if (isSavingRef.current) return;
    if (currentExercise.sets.length <= 1) return;
    if (!currentExercise.sets.some(set => set.id === setId)) return;

    markExerciseDirty(currentExercise.id);
    
    setSession((prev)=>{
      if(!prev)
        return prev;
      return{
        ...prev,
        exercises: prev.exercises.map((exercise)=>
        {
          if(exercise.id !== currentExercise.id)
            return exercise

          if(exercise.sets.length<=1)
            return exercise
          return {
            ...exercise,
            sets: exercise.sets.filter((set)=> set.id !==setId)
            .map((newSet,index)=> (
            {
              ...newSet,
              setNumber: index+1
            }))
          }
        })
      }
    })
  }

  async function addExerciseToCurrentSession(exerciseId:string) {
    if(isSavingRef.current==true) return;
    if (!session) return;

    const existingIndex = session.exercises.findIndex(
      x => x.exerciseId === exerciseId
    );
    if (existingIndex !== -1) {
      goToExercise(existingIndex);
      return;
    }
    setErrorMessage(null);
    isSavingRef.current=true;
    setIsSaving(true);
    const newExerciseIndex = session.exercises.length;
    try
    {
    const saved = await saveCurrentExerciseIfDirty();
    if (!saved)
    {
      setErrorMessage("Nie udało się zapisać aktualnego ćwiczenia.");
      return;
    } 
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

      setCurrentExerciseIndex(newExerciseIndex);
    }
    catch (error) {
      console.error("Błąd podczas dodawania ćwiczenia:", error);
      setErrorMessage("Wystąpił nieoczekiwany błąd. Spróbuj ponownie.");
    }
    finally
    {
      isSavingRef.current=false;
      setIsSaving(false);
    }
  }

  async function deleteExercise(id: string): Promise<void> {
    if(isSavingRef.current == true) return;
    if(!session) return;
    if(!id) return;
    if(!session.exercises.some((e)=>e.id==id)) return;
    const confirmed = window.confirm(
    `Czy na pewno chcesz usunąć ćwiczenie "${currentExercise.exerciseName}" z tej sesji?`
      );
    setIsExerciseMenuOpen(false);
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
    const newIndex = Math.min(currentExerciseIndex, newExercises.length - 1)
    setSession((prev)=>{
      if(!prev) 
        return prev;

        return {
          ...prev,
          exercises: newExercises
        };
      });
      setCurrentExerciseIndex(newIndex);
    } catch (error) {
        console.error(error);
        setErrorMessage("Wystąpił nieoczekiwany błąd. Spróbuj ponownie.");
    } finally {
        isSavingRef.current = false;
        setIsSaving(false);
    }
    
  }
  function handleSwipeEnd() {
    const startX = touchStartX.current;
    const endX = touchEndX.current;
    const startY = touchStartY.current;
    const endY = touchEndY.current
    if (endX == null || startX == null || startY == null || endY == null) {
      return;
    }

    const diffX = endX - startX;
    const diffY = endY - startY;
    touchEndX.current = null;
    touchStartX.current = null;
    touchEndY.current = null;
    touchStartY.current = null;
    if(Math.abs(diffX)<Math.abs(diffY))
      return;
    if (diffX < -70)
        goToExercise(currentExerciseIndex + 1);
    else if (diffX > 70)
        goToExercise(currentExerciseIndex - 1);

  }

  return (
    <section className="page">
      <div className="refresh-row">
        <button className="btn btn-secondary" onClick={() => navigate("/")}>
            ← Wróć
        </button>
        <button  type="button" aria-label="Odśwież dane sesji" title="Odśwież dane sesji"
        onClick={refreshSession}
        disabled={isSaving}
      >
        <RefreshCw size={18} />
      </button>
      </div>

<div className="touchArea" onTouchStart={(e)=>
  {
    touchStartX.current = e.touches[0].clientX;
    touchStartY.current = e.touches[0].clientY;
  }}
  onTouchEnd={(e)=>{
    touchEndX.current = e.changedTouches[0].clientX;
    touchEndY.current = e.changedTouches[0].clientY;
    handleSwipeEnd();
  }}>
  <header className="workout-session-header">
      <p>Ćwiczenie {currentExerciseIndex + 1} z {exercises.length}</p>
    
    <div className="exercise-header-row">
      <h1>{currentExercise.exerciseName}</h1>
      <div>     
      <button onClick={() => setIsExerciseMenuOpen(prev => !prev)}>...</button>     
          {isExerciseMenuOpen && (
        <div className="exercise-dropdown">
          <button disabled={isSaving} onClick={() => {deleteExercise(currentExercise.id)}}>Usuń ćwiczenie</button>
        </div>     
      )}
    </div>
    </div>
  </header>

<article className="exercise-card">


    <div className="sets-list">
      <div className="set-row">
      <div className="set-field">
        Seria
      </div>
            <div className="set-field">
        Ciężar
      </div>
      <div className="set-field">
        Powtórzenia
      </div>
      <div></div>
      </div>
      {currentExercise.sets.map(set => (
        <div className="set-row" key={set.id}>
          
          <div className="set-field">
            {set.setNumber}
          </div>

          <div className="set-field">
            <input
              value={set.weight ?? ""}
              placeholder="kg"
              inputMode="decimal"
              disabled={isSaving}
              onChange={(e) => {
                const value = e.target.value === "" ? null : Number(e.target.value);
                updateSetValue(set.id, "weight", value);
              }}
            />
          </div>

          <div className="set-field">
            <input
              value={set.reps ?? ""}
              inputMode="numeric"
              disabled={isSaving}
              placeholder="reps"
              onChange={(e) => {
                const value = e.target.value === "" ? null : Number(e.target.value);
                updateSetValue(set.id, "reps", value);
              }}
            />
          </div>
          <div>
            <button aria-label={`Usuń serię ${set.setNumber}`} onClick={
              ()=>
                {
                  deleteSet(set.id);
                }} 
              disabled={currentExercise.sets.length <= 1 || isSaving}>🗑️</button>
          </div>
        </div>
      ))}
    </div>
    <div>
      <button disabled={isSaving} className="btn btn-primary" onClick={() => 
        {
          addSet();
        }}> + Dodaj serie</button>
    </div>
  </article>
</div>
  <div>{errorMessage}</div>
  <nav className="exercise-navigation">
    
    <button onClick={() => goToExercise(currentExerciseIndex - 1)} disabled={currentExerciseIndex === 0 || isSaving}>
      ← Poprzednie
    </button>
    <button onClick={() => goToExercise(currentExerciseIndex + 1)} disabled={currentExerciseIndex === exercises.length - 1 || isSaving}>
      Następne →
    </button>
  </nav>
  <div>
    <button disabled= {isSaving} className="btn btn-secondary" onClick={() => setExerciseModalMode("picker")}>
        + Dodaj ćwiczenie
    </button>
  <button disabled={isSaving} className="btn btn-primary finish-workout-button" onClick={() => finishSession()}>
    Zakończ trening
    </button>
    <ExercisePickerModal
    isOpen={exerciseModalMode === "picker"}
    onClose={() => setExerciseModalMode("closed")}
    onCreateNew={() => setExerciseModalMode("create")}
    onSelect={(exercise) => {
      addExerciseToCurrentSession(exercise.id);

     }}
    />
  </div>
   

    <CreateExerciseModal
    isOpen={exerciseModalMode === "create"}
    onClose={() => setExerciseModalMode("closed")}
    onCreated={(exercise) => {
      addExerciseToCurrentSession(exercise.id);
      setExerciseModalMode("closed");
    }}
    />



</section>
  );
}
