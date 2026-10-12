import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";

type WorkoutSession = {
  id: string;
  workoutTemplateId: string | null;
  exercises: WorkoutSessionExercise[];
  startedAt: string;
  finishedAt: string | null;
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

type ExerciseSummary = {
  name: string;
  totalReps: number;
  totalVolume: number;
  setCount: number;
  sessionExerciseId: string;
};

async function fetchSession(id: string): Promise<WorkoutSession> {
  const response = await fetchApi(`/api/workout-sessions/${id}`);
  if (!response.ok) {
    throw new Error("Błąd przy pobieraniu sesji.");
  }

  return response.json();
}

export default function WorkoutSessionSummaryPage() {
  const navigate = useNavigate();
  const { id } = useParams();

  const [session, setSession] = useState<WorkoutSession | null>(null);

  useEffect(() => {
    console.log(id);

    if (!id) return;

    async function initSession(sessionId: string) {
      const data = await fetchSession(sessionId);
      setSession(data);
    }

    initSession(id);
  }, [id]);

  if (!session) {
    return <p>Ładowanie...</p>;
  }

  const duration = session.finishedAt
  ? Math.floor(
      (new Date(session.finishedAt).getTime() -
        new Date(session.startedAt).getTime()) / 1000
    )
  : null;


  const exercisesSummaries = session.exercises.map(
    (exer): ExerciseSummary => ({
      name: exer.exerciseName,
      totalReps: exer.sets.reduce(
        (acc, currentValue) => acc + (currentValue.reps ?? 0),
        0,
      ),
      totalVolume: exer.sets.reduce(
        (acc, currentValue) =>
          acc + (currentValue.weight ?? 0) * (currentValue.reps ?? 0),
        0,
      ),
      setCount: exer.sets.length,
      sessionExerciseId: exer.id,
    }),
  );

  const totalSeries = exercisesSummaries.reduce(
    (prev, currentValue) => prev + currentValue.setCount,
    0,
  );

  const totalReps = exercisesSummaries.reduce(
    (prev, currentValue) => prev + currentValue.totalReps,
    0,
  );

  return (
    <section className="page">
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>

      <div>
        <div className="card summary-card">
          <h2>Podsumowanie treningu!</h2>

          <div className="stat-list">
            <div className="stat-row">
              <span>Czas [s]:</span>
              <strong>{duration !== null ? duration : "Trening trwa"}</strong>
            </div>

            <div className="stat-row">
              <span>Serie:</span>
              <strong>{totalSeries}</strong>
            </div>

            <div className="stat-row">
              <span>Powtórzenia:</span>
              <strong>{totalReps}</strong>
            </div>
          </div>
        </div>

        {exercisesSummaries.map((x) => (
          <div key={x.sessionExerciseId} className="card">
            <h2>{x.name}</h2>

            <div className="stat-list">
              <div className="stat-row">
                <span>Objętość:</span>
                <strong>{x.totalVolume}</strong>
              </div>

              <div className="stat-row">
                <span>Serie:</span>
                <strong>{x.setCount}</strong>
              </div>

              <div className="stat-row">
                <span>Powtórzenia:</span>
                <strong>{x.totalReps}</strong>
              </div>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
