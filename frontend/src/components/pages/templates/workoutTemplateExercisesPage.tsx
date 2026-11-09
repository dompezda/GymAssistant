import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";
import ExercisePickerModal from "../../exercises/ExercisePickerModal";
import "./workoutTemplatesPage.css";
type SelectedExercise = {
  exerciseId: string;
  exerciseName: string;
  plannedSets: number;
};



type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};

type ActiveModal = "none" | "picker" | "create";


export default function WorkoutTemplateExercisesPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as { title?: string; notes?: string, addedExercises?:SelectedExercise[] } | null;
  const [activeModal,setActiveModal] = useState<ActiveModal>("none");
  const [addedExercises, setAddedExercises] =
  useState<SelectedExercise[]>(
    state?.addedExercises ?? []
  );
  const [isSaving,setIsSaving] = useState<boolean>(false);
  const isSavingRef = useRef(false);
  const [error,setError] = useState<string>();
  useEffect(() => {
    if (!state?.title) {
      navigate("/workout-templates/create");
      return;
    }

},[state?.title, navigate]);

  if (!state?.title) {
    return null;
  }

  function changeSets(exerciseId: string, delta: number) {
    setAddedExercises((current) =>
      current.map((x) =>
        x.exerciseId === exerciseId
          ? { ...x, plannedSets: Math.max(1, x.plannedSets + delta) }
          : x
      )
    );
  }

  function removeExercise(exerciseId: string) {
    setAddedExercises((current) =>
      current.filter((x) => x.exerciseId !== exerciseId)
    );
  }
  function addExercise(exercise:Exercise) :SelectedExercise
  {

    return {
      exerciseId: exercise.id,
      exerciseName: exercise.name,
      plannedSets:3,
    }
  }
  async function handleSubmit(event: React.FormEvent) {
    
    event.preventDefault();
    if (isSavingRef.current == true) return;
    setError(undefined);
    if (!state?.title) return;
    if (addedExercises.length === 0) {
      setError("Dodaj przynajmniej jedno ćwiczenie.");
      return;
    }
    try
    {
      isSavingRef.current=true;
      setIsSaving(true);
      const response = await fetchApi("/api/workout-templates",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title: state.title,
          notes: state.notes,
          exercises: addedExercises.map((x,index) => ({
            exerciseId: x.exerciseId,
            plannedSets: x.plannedSets,
            order: index+1,
          })),
        }),
      }
    );

    if (!response.ok) {
      setError("Nie udało się zapisać planu. Spróbuj ponownie.");
      return;
    }

    navigate("/workout-templates");
  
    }
    catch
    {
      setError("Nie udało się zapisać planu. Spróbuj ponownie.");
    }
    finally
    {
      isSavingRef.current = false;
      setIsSaving(false);
    }
  }

  function handleAddExercise(exercise: Exercise) {
  setAddedExercises(current => {
    if (current.some(x => x.exerciseId === exercise.id)) {
      return current;
    }

    return [...current, addExercise(exercise)];
  });

  setActiveModal("none");
}

  return (
    <section className="page stack templates-page">
      <form onSubmit={handleSubmit} className="template-form">
        <div className="page-header">
        <button
          type="button"
          className="btn back-button"
          disabled={isSaving}
          onClick={() =>
            navigate("/workout-templates/create", {
              state: {
                title: state.title,
                notes: state.notes,
                addedExercises
              },
            })
          }
        >
          ← Wróć
        </button>
          <h1 className="page-title">Wybierz ćwiczenia</h1>
          <p className="template-name">Plan: {state.title}</p>
          {error && <p className="error" role="alert">{error}</p>}
        </div>
        <div className="template-form-content">
        <button
          type="button"
          disabled={isSaving}
          className="btn btn-secondary"
          onClick={() => setActiveModal("picker")}
        >
          + Dodaj ćwiczenie
        </button>
        <div className="template-exercises">
          {addedExercises.map((exercise) => {
            return (
              <div className="card"
                key={exercise.exerciseId}
              >

                <div className="card-row">
                  <div className="exercise-name">{exercise.exerciseName}</div>
                   <button
                      disabled={isSaving}
                      type="button"
                      className="btn-remove"
                      aria-label={`Usuń ${exercise.exerciseName} z planu`}
                      onClick={() => removeExercise(exercise.exerciseId)}
                    >
                      ✕
                    </button>
                </div>
                

                
                  <div className="card-row">
                    <button
                      type="button"
                      className="btn-set-control"
                      
                      disabled={exercise.plannedSets <= 1 || isSaving}  
                      onClick={()=>{
                        changeSets(exercise.exerciseId, -1);
                      }}
                    >
                      -
                    </button>

                    <span>Serie: {exercise.plannedSets}</span>

                    <button
                      type="button"
                      className="btn-set-control"
                      disabled={isSaving}
                      onClick={() => {
                        changeSets(exercise.exerciseId, 1);
                      }}
                    >
                      +
                    </button>
                  </div>
                
              </div>
            );
          })}
        </div>
        </div>
        <div className="template-form-actions">
        <button type="submit" className="btn btn-primary" disabled={isSaving}>
          Zapisz plan
        </button>
        </div>
      </form>

        <ExercisePickerModal
         isOpen={activeModal=="picker"}
         onClose={()=>setActiveModal("none")}
         onSelect={handleAddExercise}
         onCreateNew={()=>{
          setActiveModal("create")
         }}
        />

        <CreateExerciseModal
        isOpen={activeModal=="create"}
        onClose={() => setActiveModal("none")}
        onCreated={handleAddExercise}
        
      />
    </section>
  );
}