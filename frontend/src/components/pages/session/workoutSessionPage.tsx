import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ExercisePickerModal from "../../exercises/ExercisePickerModal";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";

type WorkoutSession = {
  id: string;
  //title?: string;
  workoutTemplateId: string | null;
  userId: string;
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

  useEffect(() => {

    if(!id) return;
    console.log("Fetching workout session with ID:", id);
    fetchApi(`http://localhost:5235/api/workout-sessions/${id}`, {
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
    markExerciseDirty(currentExercise.id);
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

  function goToExercise(index: number): void {
    if (index < 0 || index >= exercises.length) return;

    if(dirtyIds.includes(currentExercise.id))
    {
      patchExerciseData(currentExercise)
        .then((x)=>
          {
            if(x.ok)
            {
              setCurrentExerciseIndex(index);
              
              setDirtyIds(dirtyIds.filter((prev) => currentExercise.id !==prev));    
            };
          })
    }
    else
      setCurrentExerciseIndex(index);
  }

  
  async function patchExerciseData (sessionExercise: WorkoutSessionExercise) : Promise<Response> {
    return  fetchApi(`http://localhost:5235/api/workout-sessions/${id}/exercises/${sessionExercise.id}`, {
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
    const dirtyExercises = exercises.filter(x => dirtyIds.includes(x.id));

    const responses = await Promise.all(
      dirtyExercises.map(exercise => patchExerciseData(exercise))
    );

    if (!responses.every(response => response.ok)) {
      console.log("Nie udało się zapisać wszystkich ćwiczeń");
      return;
    }

    const finishResponse = await fetchApi(`http://localhost:5235/api/workout-sessions/${id}/finish`, {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json"
      }
    });

    if (!finishResponse.ok) {
      console.log("Nie udało się zakończyć sesji");
      return;
    }

    console.log("Sesja zakończona");
    navigate(`/workout-sessions/${id}/summary`)
  }

  function markExerciseDirty(sessionExerciseId: string) {
  setDirtyIds(prev =>
    prev.includes(sessionExerciseId)
      ? prev
      : [...prev, sessionExerciseId]
  );
}

  function addSet(): void {


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
        markExerciseDirty(currentExercise.id);

      return {
        ...exercise,
        sets:
        [
          ...exercise.sets,
          newSet
        ]
      }
    })}})
  }

  function deleteSet(setId: string): void {
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

          markExerciseDirty(currentExercise.id);
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
    if (!session) return;

    const existingIndex = session.exercises.findIndex(
      x => x.exerciseId === exerciseId
    );
    if (existingIndex !== -1) {
      setCurrentExerciseIndex(existingIndex);
      return;
    }
    const newExerciseIndex = session.exercises.length;
    const response = await fetchApi(`http://localhost:5235/api/workout-sessions/${session.id}/exercises`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        exerciseId: exerciseId
      })
    });
    if (!response.ok) {
      const error = await response.text();
      console.error(error);
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
      
      if(session.exercises.length===1)
        setCurrentExerciseIndex(0)
      else  
        setCurrentExerciseIndex(newExerciseIndex);
  }

  async function deleteExercise(id: string): Promise<void> {
    if(!session) return;
    if(!id) return;
    if(!session.exercises.some((e)=>e.id==id)) return;

    console.log(id);
    const confirmed = window.confirm(
    `Czy na pewno chcesz usunąć ćwiczenie "${currentExercise.exerciseName}" z tej sesji?`
      );

    if (!confirmed) {
      return;
    }

    const response = await fetchApi(`http://localhost:5235/api/workout-sessions/${session.id}/exercises/${id}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json"
      }
    });

    if(!response.ok)
    {
      console.log(`Błąd przy usuwaniu ${response.body}`);
      return;
    }
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

  }

  return (
    <section className="page">
  <button className="btn btn-secondary" onClick={() => navigate("/")}>
    ← Wróć
  </button>

  <header className="workout-session-header">
    <p>Ćwiczenie {currentExerciseIndex + 1} z {exercises.length}</p>
    <h1>{currentExercise.exerciseName}</h1>
  </header>

<article className="exercise-card">

    <button className="btn btn-danger" disabled={exercises.length <= 1} onClick={() => deleteExercise(currentExercise.id)}>
        - Usuń ćwiczenie
    </button>
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
          <div>
            <button onClick={()=>deleteSet(set.id)} disabled={currentExercise.sets.length <= 1}> Usuń serie</button>
            </div>
        </div>
      ))}
    </div>
    <div>
      <button className="btn btn-primary" onClick={() => addSet()}> + Dodaj serie</button>
    </div>

  </article>

  <nav className="exercise-navigation">
    <button onClick={() => goToExercise(currentExerciseIndex - 1)} disabled={currentExerciseIndex === 0}>
      ← Poprzednie
    </button>
    <button onClick={() => goToExercise(currentExerciseIndex + 1)} disabled={currentExerciseIndex === exercises.length - 1}>
      Następne →
    </button>
  </nav>
  <div>
    <button className="btn btn-secondary" onClick={() => setExerciseModalMode("picker")}>
        + Dodaj ćwiczenie
    </button>
  <button className="btn btn-primary finish-workout-button" onClick={() => finishSession()}>
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
