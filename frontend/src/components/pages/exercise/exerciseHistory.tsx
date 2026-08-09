import { useEffect, useState } from "react"
import { useParams } from "react-router-dom";
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


export default function ExerciseHistory(){

const { id} = useParams();
const [exercise,setExercise] = useState<Exercise | null>(null);
const [isLoading, setIsLoading] = useState(true);

const [error, setError] = useState<string | null>(null);



useEffect(() => {
  async function fetchExerciseById(): Promise<Exercise> {
    const response = await fetchApi(
      `http://localhost:5235/api/exercises/${id}/history`
    );

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
        <section>

            <div>
                {exercise.name}
            </div>
        {exercise.workouts.map((workout) => (
            <div
              key={workout.id}
              className="card summary-card"
            >
              {formatWorkoutDate(workout.workoutDate)}
              <div className="stat-list">
                {workout.sets.toSorted((a, b) => a.setNumber - b.setNumber).map((set)=>(
                    <div key={set.id}>
                        <div className="stat-row">
                            {`Seria: ${set.setNumber} `}
                            {set.reps !== null ? `${set.reps}x` : "brak powtórzeń"}
                            {set.weight !== null ? `${set.weight}kg` : "brak ciężaru"}
                        </div>
                   </div>
                ))}
              </div>
            </div>
          ))}
        </section>
    )
}