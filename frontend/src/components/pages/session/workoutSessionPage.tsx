import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";

type WorkoutSession = {
  id: string;
  //title?: string;
  workoutTemplateId: string | null;
  userId: string;
  date: string;
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

export default function WorkoutSessionPage() {
  //const navigate = useNavigate();
  const { id } = useParams();
  const [session, setSession] = useState<WorkoutSession | null>(null);
  const [dirtyIds, setDirtyIds] = useState<string[]>([]); 
  const exercises = session?.exercises ?? [];
  const [currentExerciseIndex, setCurrentExerciseIndex] = useState(0);
  const currentExercise = exercises[currentExerciseIndex];
  useEffect(() => {

    if(!id) return;
    console.log("Fetching workout session with ID:", id);
    fetch(`http://localhost:5235/api/workout-sessions/${id}`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json"
      }
    })
      .then(async response => {
  if (!response.ok) {
    console.error(await response.text());
    return;
  }

      return response.json();
        })      .then(data => {
            setSession(data);
            setCurrentExerciseIndex(0);
          })
          

  }, [id]);

  if (!session) {
  return <p>Ładowanie sesji...</p>;
}

if (exercises.length === 0 || !currentExercise) {
  return <p>Brak ćwiczeń w tej sesji.</p>;
}
  function updateSetValue(
  setId: string,
  field: "weight" | "reps",
  value: number | null
) {
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
            console.log(dirtyIds);
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

  return (
    <section className="workout-session-page">
  <button>← Wróć</button>

  <header className="workout-session-header">
    <p>Ćwiczenie {currentExerciseIndex + 1} z {exercises.length}</p>
    <h1>{currentExercise.exerciseName}</h1>
  </header>

<article className="exercise-card">
    <h2 className="exercise-name">{currentExercise.exerciseName}</h2>

    <div className="sets-list">
      {currentExercise.sets.map(set => (
        <div className="set-row" key={set.id}>
          <div className="set-number">Seria {set.setNumber}</div>

          <div className="set-field">
            <label>Ciężar</label>
            <input
              value={set.weight ?? ""}
              placeholder="kg"
              onChange={(e) => {
                const value = e.target.value === "" ? null : Number(e.target.value);
                updateSetValue(set.id, "weight", value);
              }}
            />
          </div>

          <div className="set-field">
            <label>Powtórzenia</label>
            <input
              value={set.reps ?? ""}
              placeholder="reps"
              onChange={(e) => {
                const value = e.target.value === "" ? null : Number(e.target.value);
                updateSetValue(set.id, "reps", value);
              }}
            />
          </div>
        </div>
      ))}
    </div>
  </article>

  <nav className="exercise-navigation">
    <button onClick={() => setCurrentExerciseIndex(prev => Math.max(0, prev - 1))}>
      ← Poprzednie
    </button>
    <button onClick={() => setCurrentExerciseIndex(prev => Math.min(exercises.length - 1, prev + 1))}>
      Następne →
    </button>
  </nav>

  <button className="btn btn-primary finish-workout-button">
    Zakończ trening
    </button>
</section>
  );
}