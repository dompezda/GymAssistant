import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
type WorkoutSession = {
  id: string;
  title: string;
  workoutTemplateId: string | null;
  userId: string;
  sessionExercises: WorkoutSessionExercise[];
  startedAt: string;
  finishedAt: string;

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

// type ExerciseSummary = 
// {
//   name :string;
//   totalReps : number;
//   totalVolume : number;
//   setCount :number;
// }



export default function WorkoutSessionSummaryPage()
{
    const navigate = useNavigate();
    const { id } = useParams();
    const [session, setSession] = useState<WorkoutSession | null>(null);
        
async function fetchSession(id: string): Promise<WorkoutSession> {
  const response = await fetch(
    `http://localhost:5235/api/workout-sessions/${id}/summary`
  );
  console.log(response);
  if (!response.ok) {
    throw new Error("Błąd przy pobieraniu sesji.");
  }
  
  return response.json();
}

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

const startTime = new Date(session.startedAt).getTime();
const endTime = new Date(session.finishedAt).getTime();
const duration = Math.floor((endTime-startTime)/1000);
const exercisesSummaries = (
  session.sessionExercises.map((exer)=> ({
    name: exer.exerciseName,
    totalReps: exer.sets.reduce((acc, currentValue) => acc + (currentValue.reps ?? 0),0),
    totalVolume: exer.sets.reduce((acc, currentValue) => acc + ((currentValue.weight ?? 0) * (currentValue.reps ?? 0)),0), 
    setCount : exer.sets.length,
    exerciseId : exer.exerciseId
  })));




    return (
        <section>
        <button className="btn btn-secondary" onClick={() => navigate("/")}>
            ← Wróć
        </button>
        <div>
            Podsumowanie
            Czas trwania treningu: {duration}
            {exercisesSummaries.map((x) => (
              <div key={x.exerciseId}>
              <h1> {x.name}</h1>
              <div> Objętość: {x.totalVolume} </div>
              <div> Łączna ilość serii: {x.setCount} </div>
              <div> Łączna ilość powtórzeń: {x.totalReps}</div>
              </div>
            ))}
        </div>
        </section>
    );
}