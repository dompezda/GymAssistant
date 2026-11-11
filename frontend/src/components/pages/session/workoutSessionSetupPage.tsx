import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import  ExercisePickerModal  from "../../exercises/ExercisePickerModal";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";

type DraftExercise = {
  exerciseId: string;
  exerciseName: string;
  sets: number;
  order: number;
};

type WorkoutTemplateDetails = {
  id: string;
  title: string;
  notes: string | null;
  exercises: {
    exerciseId: string;
    exerciseName: string;
    plannedSets: number;
    order: number;
  }[];
};

type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};
type ExerciseModalMode = "closed" | "picker" | "create";

export default function WorkoutSessionSetupPage() {
const navigate = useNavigate();
const { templateId } = useParams();
const [templateTitle, setTemplateTitle] = useState<string | null>(null);
const [exercises, setExercises] = useState<DraftExercise[]>([]);
const [loading, setLoading] = useState(false);
const [exerciseModalMode, setExerciseModalMode] = useState<ExerciseModalMode>("closed");
const [isSaving,setIsSaving] = useState<boolean>(false);
const isSavingRef = useRef<boolean>(false);
const [error, setError] = useState<string | null>(null);

const title = templateId
  ? templateTitle ?? "Trening z planu"
  : "Pusty trening";


  function mapExercisesToDraft(exercises: Exercise): DraftExercise {
      const draftExercise: DraftExercise = {
          exerciseId: exercises.id,
          exerciseName: exercises.name,
          sets: 3,
          order: 1,
      };
      return draftExercise;
  }
useEffect(() => {
  if (!templateId) {
    return;
  }

  async function loadTemplate() {
    setLoading(true);

    const response = await fetchApi(
      `/api/workout-templates/${templateId}`
    );

    if (!response.ok) {
      console.error(await response.text());
      setLoading(false);
      return;
    }

    const data: WorkoutTemplateDetails = await response.json();

    setTemplateTitle(data.title);
    setExercises(
      data.exercises.map((x) => ({
        exerciseId: x.exerciseId,
        exerciseName: x.exerciseName,
        sets: x.plannedSets,
        order: x.order,
      }))
    );

    setLoading(false);
  }

  loadTemplate();
}, [templateId]);

function increaseSets(exerciseId: string) {
  setExercises((current) =>
    current.map((exercise) =>
      exercise.exerciseId === exerciseId
        ? { ...exercise, sets: exercise.sets + 1 }
        : exercise
    )
  );
}

function decreaseSets(exerciseId: string) {
  setExercises((current) =>
    current.map((exercise) =>
      exercise.exerciseId === exerciseId
        ? { ...exercise, sets: Math.max(1, exercise.sets - 1) }
        : exercise
    )
  );
}

function removeExercise(exerciseId: string) {
  setExercises((current) =>
    current.filter((exercise) => exercise.exerciseId !== exerciseId)
  );
}

async function handleSubmit(
  event: React.FormEvent<HTMLFormElement>
): Promise<void> {
  event.preventDefault();
  if(isSavingRef.current == true) return;
  isSavingRef.current=true;
  setIsSaving(true);
  setError(null);
  try
  {
    const request = {
      workoutTemplateId: templateId ?? null,
      exercises: exercises.map((x) => ({
        exerciseId: x.exerciseId,
        sets: Array.from({ length: x.sets }, (_, index) => ({
          setNumber: index + 1,
          weight: null,
          reps: null,
        })),
      })),
    };

    const response = await fetchApi("/api/workout-sessions", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    const data = await response.json();
    navigate(`/workout-sessions/${data.id}`);
  }
  catch {
    setError("Nie udało się rozpocząć treningu. Spróbuj ponownie.");
  }
  finally
  {
    setIsSaving(false);
    isSavingRef.current=false;
  }
  
    
  }

  function addExercise(exercise: Exercise): void {
  const draftExercise = mapExercisesToDraft(exercise);

  setExercises((current) => {
    if (current.some((x) => x.exerciseId === exercise.id)) {
      return current;
    }

    return [
      ...current,
      {
        ...draftExercise,
        order: current.length + 1,
      },
    ];
  });

  setExerciseModalMode("closed");
}

  return (
     <section className="page stack templates-page">
      <div className="page-header">
          <button className="btn back-button" onClick={() => navigate("/")}>
            ← Wróć
          </button>
          <header>
            <h1 className="page-title">{title}</h1>
          </header>
       </div>
      
{loading ? (
  <p>Ładowanie planu...</p>
) : (
  <form className="template-form" onSubmit={handleSubmit}>
     <div className="session-setup-toolbar">
    <button
      type="button"
      className="btn btn-secondary"
      onClick={() => setExerciseModalMode("picker")}
    >
      + Dodaj ćwiczenie
    </button>
    {error && <p className="error">{error}</p>}
  </div>   
    <div className="template-form-content">

      {exercises.length === 0 ? (
        <p>Dodaj przynajmniej jedno ćwiczenie, aby rozpocząć trening.</p>
      ) : (
        <div className="template-exercises">
          {[...exercises]
            .sort((a, b) => a.order - b.order)
            .map((exercise) => (
              <div className="card" key={exercise.exerciseId}>
                <div className="card-row">
                  <div className="exercise-name">
                    {exercise.exerciseName}
                  </div>

                  <button
                    type="button"
                    className="btn-remove"
                    aria-label={`Usuń ${exercise.exerciseName} z sesji`}
                    onClick={() => removeExercise(exercise.exerciseId)}
                  >
                    ✕
                  </button>
                </div>

                <div className="card-row">
                  <button
                    type="button"
                    className="btn-set-control"
                    disabled={exercise.sets <= 1}
                    onClick={() => decreaseSets(exercise.exerciseId)}
                  >
                    -
                  </button>

                  <span>Serie: {exercise.sets}</span>

                  <button
                    type="button"
                    className="btn-set-control"
                    onClick={() => increaseSets(exercise.exerciseId)}
                  >
                    +
                  </button>
                </div>
              </div>
            ))}
        </div>
      )}
    </div>

    <div className="template-form-actions">
      <button
        type="submit"
        className="btn btn-primary"
        disabled={isSaving || exercises.length === 0}
      >
        Rozpocznij sesję
      </button>
    </div>
  </form>
)}

      <ExercisePickerModal
        isOpen={exerciseModalMode === "picker"}
        onClose={() => setExerciseModalMode("closed")}
        onCreateNew={() => setExerciseModalMode("create")}
        onSelect={addExercise}
      />
      <CreateExerciseModal
        isOpen={exerciseModalMode === "create"}
        onClose={() => setExerciseModalMode("picker")}
        onCreated={addExercise}
      />
    </section>
  );
}