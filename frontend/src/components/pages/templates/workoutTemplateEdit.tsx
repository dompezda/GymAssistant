import { useEffect, useState } from "react"
import { useNavigate, useParams } from "react-router-dom";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";
type Template={
  id: string;
  title: string;
  notes: string | null;
  exercises: SelectedExercise[];
}
type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};
type SelectedExercise = {
  exerciseId: string;
  exerciseName: string;
  plannedSets: number;
  order: number;
};


export default function WorkoutTemplateEdit() {
const navigate = useNavigate();
const { id } = useParams<{ id: string }>();
//const [template, setTemplate] = useState<Template | null>(null);
const [exercises, setExercises] = useState<Exercise[]>([]);
const [title, setTitle] = useState<string>("");
const [notes, setNotes] = useState<string | null>(null);
const [selectedExercises, setSelectedExercises] = useState<SelectedExercise[]>([]);
const [isCreateExerciseModalOpen, setIsCreateExerciseModalOpen] = useState(false);
const [searchText, setSearchText] = useState("");
const [error, setError] = useState<string | null>(null);
async function fetchExercises(): Promise<Exercise[]> {
    const response = await fetchApi("http://localhost:5235/api/exercises");

      if (!response.ok) {
        const error = await response.text();
        console.error(error);
        return [];
      }

      const data = await response.json();
      return data;
  }
async function fetchTemplate(templateId: string): Promise<Template | null> {
    const response = await fetchApi(`http://localhost:5235/api/workout-templates/${templateId}`);
    if(!response.ok) {
      const error = await response.text();
      console.error(error);
      return null;
    }
    const data = await response.json();
    return data;
}

useEffect(()=>
{
  if(!id) return;

  let ignore = false;

  async function loadInitialExercises() {
    const data = await fetchExercises();

    if (!ignore) {
      setExercises(data);
    }
  }

  async function loadTemplate() {
    if(!id) return;
    const data = await fetchTemplate(id);
    if(!ignore && data) {
      //setTemplate(data);
      setTitle(data.title);
      setNotes(data.notes);
      setSelectedExercises(data.exercises);
    }
  }

  loadInitialExercises();
  loadTemplate();
  return () => {
    ignore = true;
  };
}, [id]);

  function changeSets(exerciseId: string, delta: number) {
    setSelectedExercises((current) =>
      current.map((x) =>
        x.exerciseId === exerciseId
          ? { ...x, plannedSets: Math.max(1, x.plannedSets + delta) }
          : x
      )
    );
  }

    function handleExerciseClick(exercise: Exercise): void {
        if(selectedExercises.some((x) => x.exerciseId === exercise.id)) {
            setSelectedExercises((current) =>
                current.filter((x) => x.exerciseId !== exercise.id)
            );
        }
        else {
            setSelectedExercises((current) => [
                ...current,
                {
                    exerciseId: exercise.id,
                    exerciseName: exercise.name,
                    plannedSets: 3,
                    order: current.length + 1,
                },
            ]);
        }
    }
const visibleExercises = exercises.filter((exercise) =>
    exercise.name.toLowerCase().includes(searchText.trim().toLowerCase())
  );

  async function handleSubmit(event: React.FormEvent): Promise<void> {
    event.preventDefault();
    setError(null);
    if(!id) {
      setError("Nieprawidłowy identyfikator planu");
      return;
    }
    if(title.trim() === "") {
      setError("Tytuł jest wymagany");
      return;
    }
    if(selectedExercises.length === 0) {
      setError("Dodaj przynajmniej jedno ćwiczenie");
      return;
    }

    const response = await fetchApi(`http://localhost:5235/api/workout-templates/${id}`, {
      method: "PATCH",
      headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title: title.trim(),
          notes: notes,
          exercises: selectedExercises.map((x, index) => ({
            exerciseId: x.exerciseId,
            plannedSets: x.plannedSets,
            order: index + 1,
          })),
        }),
      });
    if (!response.ok) {
      const error = await response.text();
      console.error(error);
      return;
    }

    navigate("/workout-templates");
  }

return(
    <section className="page">
        <div className="stack">
    <button
        className="btn btn-secondary"
        onClick={() => navigate("/workout-templates")}
      >
        ← Wróć
      </button>
      <form className="stack" onSubmit={handleSubmit}>
        {error && <p className="error">{error}</p>}
            <div>
                <label>
                    Tytuł
                    <input
                    type="text"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    required
                />
                </label>
                
            </div>
            <div>
                <label>
                    Notatki
                                 <textarea
              name="notes"
              value={notes ?? ""}
              onChange={(e) => setNotes(e.target.value)}
            ></textarea>
                </label>

            </div>
            <div>
              <button type="button" className="btn btn-primary" onClick={() => setIsCreateExerciseModalOpen(true)}>
                + Dodaj ćwiczenie
              </button>
            </div>
            <div>
              <input type="text" placeholder="Szukaj ćwiczenia..." value={searchText} onChange={(e) => setSearchText(e.target.value)} />
            </div>
            <div>

                {visibleExercises.map((exercise) => {
                    const selected = selectedExercises.find(
                        selected => selected.exerciseId === exercise.id
                    );
                    return (
                        <div onClick={() => handleExerciseClick(exercise)} className={`card ${selected ? "card-selected" : ""}`} key={exercise.id}>{exercise.name} 
                    {exercise.muscleGroupLabels.length > 0 && (
                    <p>{exercise.muscleGroupLabels.join(", ")}</p>
                    )}
                    
                    {selected && (   
                  <div className="sets-row">
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        changeSets(selected.exerciseId, -1);
                      }}
                    >
                      -
                    </button>

                    <span>Serie: {selected.plannedSets}</span>

                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        changeSets(selected.exerciseId, 1);
                      }}
                    >
                      +
                    </button>
                  </div>
                )}                
            </div>
                    )})}
        <button type="submit" className="btn btn-primary">
          Zapisz plan
        </button>  
            </div>
            </form>
        </div>

        <CreateExerciseModal
        isOpen={isCreateExerciseModalOpen}
        onClose={() => setIsCreateExerciseModalOpen(false)}
        onCreated={async (exercise) => {
          setIsCreateExerciseModalOpen(false);
          
          const data = await fetchExercises();
          setExercises(data);
          
          setSelectedExercises((current) => [
                ...current,
                {
                    exerciseId: exercise.id,
                    exerciseName: exercise.name,
                    plannedSets: 3,
                    order: current.length + 1,
                },
            ]);
        }}
        
      />
    </section>
)
}