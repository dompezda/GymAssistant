import { useEffect, useRef, useState } from "react"
import { useNavigate, useParams} from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";

type Exercise =
{
    name: string,
    id: string,
    workouts:ExerciseHistoryWorkoutResponse[]
}

type SessionError = {
  sessionId: string;
  message: string;
};

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
const [cachedSessions,setCachedSessions] = useState<Map<string,WorkoutSession>>(new Map());
const [currentOpenedSession,setCurrentOpenedSession] = useState<string | null>(null);
const isFetchingRef = useRef(false);
const [isFetchingSession, setIsFetchingSession] = useState(false);
const [sessionError,setSessionError] = useState<SessionError |null>(null);
const currentSession =
    currentOpenedSession !== null
      ? cachedSessions.get(currentOpenedSession) ?? null
      : null;
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
  if(isFetchingRef.current == true) return;
  if(!workoutSessionId)
    return;
  setSessionError(null);
  setIsFetchingSession(true);
  isFetchingRef.current=true;
  try
  {
    const res = await fetchApi(`/api/workout-sessions/${workoutSessionId}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        }
      })
      if(!res.ok)
      {
        const err :SessionError = {
          sessionId: workoutSessionId,
          message:"Błąd przy odczycie sesji z serwera. Spróbuj ponownie."
        }
        setSessionError(err);
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
    catch {
      setSessionError({
        sessionId: workoutSessionId,
        message: "Nie udało się pobrać szczegółów treningu."
      });
    }
    finally
    {
      setIsFetchingSession(false);
      isFetchingRef.current=false;
    }
    
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
          {sessionError?.sessionId === workout.id && (
            <p className="error">{sessionError.message}</p>
          )}            
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
                {currentOpenedSession !== workout.id &&
                <div className="card-link">
                  <button 
                  className="details-button"
                  disabled={isFetchingSession}
                    onClick={()=>
                    {
                      const cachedSession = cachedSessions?.get(workout.id);
                      if(cachedSession !== undefined)
                        setCurrentOpenedSession(cachedSession.id)
                      else
                        getSession(workout.id)
                    }}>
                      Pokaż szczegóły
                  </button>
                </div>
                }
              </div>

              {currentOpenedSession &&  
              currentOpenedSession === workout.id &&
              <>
              <div className="session-list">
              <div className="session-exercises">  
              <div className="header">
                    Nazwa
                </div>
                <div className="header" >
                  Serie
                </div>
              </div>
              {currentSession?.exercises.map((exercise)=>
              {
                const isCurrent = exercise.exerciseId === id;
                return (
                  <div key={exercise.id} className="session-exercises">  
                    <div className={isCurrent ? "current-exercise" : "exercise-name"}>
                        {exercise.exerciseName}
                    </div>
                    <div className="exercise-sets">
                      {exercise.sets.length}
                    </div>
                  </div>
                )
                
              })}
              <button disabled={isFetchingSession} className="details-button" onClick={()=>setCurrentOpenedSession(null)}>
                Ukryj szczegóły
              </button>
              </div>
              </>
            }
              
            </article>
            
          ))}
        </section>
    )
}