import { useEffect, useState } from "react"
import { useNavigate, useParams} from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";

type Exercise =
{
    name: string,
    id: string,
    workouts:ExerciseHistoryWorkoutResponse[]
}

type ExerciseHistoryWorkoutResponse =
{
    id: string,
    workoutDate:string,
    sets:WorkoutSet[]
}
type WorkoutSet =
{
    id:string,
    reps: number | null,
    setNumber:number,
    weight:number | null
}


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

export default function ExerciseHistory(){

const { id} = useParams();
const [exercise,setExercise] = useState<Exercise | null>(null);
const [isLoading, setIsLoading] = useState(true);

const [error, setError] = useState<string | null>(null);
const navigate = useNavigate()
const [cachedSessions,setCachedSessions] = useState<Map<string,WorkoutSession>|null>(null);
const [currentOpenedSession,setCurrentOpenedSession] = useState<string | null>();
useEffect(() => {
  async function fetchExerciseById(): Promise<Exercise> {
    const response = await fetchApi(`/api/exercises/${id}/history`);

    if (!response.ok) {
      const errorMessage = await response.text();
      throw new Error(errorMessage);
    }

    return await response.json();
  }

  async function loadExercise() {
    setIsLoading(true);
    setError(null);

    try {
      if (!id) {
        throw new Error("Brak identyfikatora ćwiczenia.");
      }

      const data = await fetchExerciseById();
      setExercise(data);
    } catch (error: unknown) {
      setExercise(null);

      if (error instanceof Error) {
        setError(error.message);
      } else {
        setError("Wystąpił nieznany błąd.");
      }
    } finally {
      setIsLoading(false);
    }
  }

  loadExercise();
}, [id]);

async function getSession(workoutSessionId:string) 
{
  if(!workoutSessionId)
    return;
      const res = await fetchApi(`/api/workout-sessions/${workoutSessionId}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        }
      })
      if(!res.ok)
      {
        //setErrorMessage("Błąd przy odczycie sesji z serwera. Spróbuj ponownie.")
        return;
      }
      const data :WorkoutSession = await res.json();
      
      setCachedSessions(((prev) =>
      {
        const newMap = new Map(prev);
        newMap.set(data.id,data);
        return newMap;
      }
      ));
      setCurrentOpenedSession(workoutSessionId);
}

function formatWorkoutDate(date: string) {
  const workoutDate = new Date(date);
  return workoutDate.toLocaleDateString("pl-PL", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

if(isLoading)
    return(
        <section>
            <div>
                Trwa ładowanie...
            </div>
        </section>
    )

    if(error){
        return(
            <div>
                {error}
            </div>
        )
    }
    if(!exercise)
    {
        return(
            <p>Nie znaleziono ćwiczenia</p>
        )
    }
    
        if (exercise.workouts.length === 0) {
        return <p>Brak zapisanych treningów dla tego ćwiczenia.</p>;
    }
    return(
    <section className="page stack exercises-page">
      <div className="page-header">
      <button className="btn back-button" onClick={() => navigate("/exercises")}>
        ← Wróć
      </button>

      <header>
        <h2 className="page-title">{exercise.name}</h2>
      </header>
      </div>
        {exercise.workouts.map((workout) => (
            <article
              key={workout.id}
              className="card" 
            >            
              <div className="stat-list">
              <div className="date-row">
                {formatWorkoutDate(workout.workoutDate)}
              </div>  
                {workout.sets.toSorted((a, b) => a.setNumber - b.setNumber).map((set)=>(
                    <div key={set.id} className="stat-row">
                          <div> {`${set.setNumber} `} </div>
                          <div> {set.reps !== null ? `${set.reps}x` : "0"} </div>
                          <div> {set.weight !== null ? `${set.weight}kg` : "0"} </div> 
                   </div>
                ))}
                <div className="card-link">
                  <button 
                    onClick={()=>
                    {
                      if(currentOpenedSession == workout.id)
                        setCurrentOpenedSession(null);
                      else
                        getSession(workout.id)
                    }}>
                      Trening
                  </button>
                </div>
              </div>
              {currentOpenedSession &&  
              currentOpenedSession === workout.id  &&
              <p>HELLO</p>}
                
            </article>
          ))}
        </section>
    )
}