import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

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


export default function WorkoutSessionSetupPage() {
const navigate = useNavigate();
const { templateId } = useParams();
const [templateTitle, setTemplateTitle] = useState<string | null>(null);
const [exercises, setExercises] = useState<DraftExercise[]>([]);
const [loading, setLoading] = useState(false);
const title = templateId
  ? templateTitle ?? "Trening z planu"
  : "Pusty trening";

useEffect(() => {
  if (!templateId) {
    return;
  }

  async function loadTemplate() {
    setLoading(true);

    const response = await fetch(
      `http://localhost:5235/api/workout-templates/${templateId}`
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

  async function startSession() {
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

    const response = await fetch("http://localhost:5235/api/workout-sessions", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      console.error(await response.text());
      return;
    }

    const data = await response.json();

    navigate(`/workout-sessions/${data.id}`);
  }

  return (
    <section>
      <button
        className="btn btn-secondary"
        onClick={() => navigate("/workout-sessions/start")}
      >
        ← Wróć
      </button>

      <h1>Przygotowanie treningu</h1>

      {loading ? (
        <p>Ładowanie planu...</p>
      ) : (
        <>
          <h2>{title}</h2>

          {exercises.length === 0 ? (
            <p>Brak ćwiczeń. Dodawanie ćwiczeń zrobisz później.</p>
          ) : (
            <div className="stack">
              {[...exercises]
                .sort((a, b) => a.order - b.order)
                .map((exercise) => (
                  <article key={exercise.exerciseId} className="card">
  <h3>{exercise.exerciseName}</h3>

  <div className="sets-row">
    <button
      type="button"
      onClick={() => decreaseSets(exercise.exerciseId)}
    >
      -
    </button>

    <span> Serie: {exercise.sets} </span>

    <button
      type="button"
      
      onClick={() => increaseSets(exercise.exerciseId)}
    >
      +
    </button>
  </div>

  <button
    className="btn btn-secondary"
    onClick={() => removeExercise(exercise.exerciseId)}
  >
    Usuń
  </button>
</article>
                ))}
            </div>
          )}

          <button
  className="btn btn-primary"
  onClick={startSession}
  disabled={exercises.length === 0}
>
  Rozpocznij sesję
</button>
        </>
      )}
    </section>
  );
}