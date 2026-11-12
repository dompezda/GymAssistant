
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

    type WorkoutSessionExerciseParams = {
        //onClose: () => void;
        isOpen:boolean;
        exercise: WorkoutSessionExercise | null;
        isSaving: boolean
        setSession: React.Dispatch<
        React.SetStateAction<WorkoutSession | null>>
        onExerciseChanged: (exerciseId: string) => void
    }

    export default function WorkoutSessionExerciseDetails({
        isOpen,
        exercise,
        isSaving,
        setSession,
        onExerciseChanged,
    }: WorkoutSessionExerciseParams) {
       
  if (!isOpen || !exercise)
    return null;
  function addSet(): void {
    if (isSaving || !exercise) return;
    onExerciseChanged(exercise.id);
    const guid = crypto.randomUUID();
    setSession((prev)=> 
    {
      if(!prev)
        return prev;

    return {
    ...prev,
    exercises: prev.exercises.map((item:WorkoutSessionExercise) =>
    {
      if (item.id !== exercise.id)
        return item;

        const newSet : WorkoutSessionSet ={
          id: guid,
          setNumber: item.sets.length+1,
          weight: null,  
          reps : null,           
        }
        //markExerciseDirty(exercise.id);
      return {
        ...item,
        sets:
        [
          ...item.sets,
          newSet
        ]
      }
    })}
    
  })
  }

  function deleteSet(setId: string): void {
    if (isSaving || !exercise) return;
    if (exercise.sets.length <= 1) return;
    if (!exercise.sets.some(set => set.id === setId)) return;
    onExerciseChanged(exercise.id);
    setSession((prev)=>{
      if(!prev)
        return prev;
      return{
        ...prev,
        exercises: prev.exercises.map((item:WorkoutSessionExercise)=>
        {
          if(item.id !== exercise.id)
            return item;

          if(item.sets.length<=1)
            return item
          return {
            ...item,
            sets: item.sets.filter((set)=> set.id !==setId)
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

  
  function updateSetValue(
  setId: string,
  field: "weight" | "reps",
  value: number | null
) {
  if (isSaving || !exercise) return;
onExerciseChanged(exercise.id);
    setSession(prev => {
    if (!prev) return prev;
    
    return {
      ...prev,
      exercises: prev.exercises.map((item:WorkoutSessionExercise) => {
        if (item.id !== exercise.id) {
          return item;
        }
        return {
          ...item,
          sets: item.sets.map(set => {
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
       
return (
  <div className="workout-session-details">
    <h2 className="exercise-title">{exercise.exerciseName}</h2>

    <article className="exercise-card">
      <div className="sets-list">
        <div className="set-row sets-header">
          <span>Seria</span>
          <span>Ciężar</span>
          <span>Powtórzenia</span>
          <span />
        </div>

        {exercise.sets.map((set) => (
          <div className="set-row" key={set.id}>
            <div className="set-field set-number">
              {set.setNumber}
            </div>

            <div className="set-field">
              <input
                aria-label={`Ciężar serii ${set.setNumber}`}
                value={set.weight ?? ""}
                placeholder="kg"
                inputMode="decimal"
                disabled={isSaving}
                onChange={(e) => {
                  const value =
                    e.target.value === ""
                      ? null
                      : Number(e.target.value);

                  updateSetValue(set.id, "weight", value);
                }}
              />
            </div>

            <div className="set-field">
              <input
                aria-label={`Powtórzenia serii ${set.setNumber}`}
                value={set.reps ?? ""}
                placeholder="reps"
                inputMode="numeric"
                disabled={isSaving}
                onChange={(e) => {
                  const value =
                    e.target.value === ""
                      ? null
                      : Number(e.target.value);

                  updateSetValue(set.id, "reps", value);
                }}
              />
            </div>

            <button
              type="button"
              className="btn-delete-set"
              aria-label={`Usuń serię ${set.setNumber}`}
              disabled={exercise.sets.length <= 1 || isSaving}
              onClick={() => deleteSet(set.id)}
            >
              🗑️
            </button>
          </div>
        ))}
      </div>

      <button
        type="button"
        className="btn btn-primary add-set-button"
        disabled={isSaving}
        onClick={addSet}
      >
        + Dodaj serię
      </button>
    </article>
  </div>
);
 }