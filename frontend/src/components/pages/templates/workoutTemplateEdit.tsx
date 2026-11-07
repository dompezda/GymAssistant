import { useEffect, useState } from "react"
import { useNavigate, useParams } from "react-router-dom";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";
import "./workoutTemplatesPage.css";
import ExercisePickerModal from "../../exercises/ExercisePickerModal";
type Template={
  id: string;
  title: string;
  notes: string | null;
  exercises: SelectedExercise[];
}
type SelectedExercise = {
  exerciseId: string;
  exerciseName: string;
  plannedSets: number;
  order: number;
};

type ActiveModal = "none" | "picker" | "create";
export default function WorkoutTemplateEdit() {
const navigate = useNavigate();
const { id } = useParams<{ id: string }>();
const [title, setTitle] = useState<string>("");
const [notes, setNotes] = useState<string | null>(null);
const [selectedExercises, setSelectedExercises] = useState<SelectedExercise[]>([]);
const [error, setError] = useState<string | null>(null);
const [activeModal,setActiveModal] = useState<ActiveModal>("none");

async function fetchTemplate(templateId: string): Promise<Template | null> {
    const response = await fetchApi(`/api/workout-templates/${templateId}`);
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

    const response = await fetchApi(`/api/workout-templates/${id}`, {
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

  function removeExercise(exerciseId: string): void {
  setSelectedExercises(current =>
    current.filter(x => x.exerciseId !== exerciseId)
  );
}

return(
    <section className="page stack templates-page">
      <div className="page-header">
    <button
        className="btn back-button"
        onClick={() => navigate("/workout-templates")}>
        ← Wróć
      </button>
      </div>
      
      <form className="template-form" onSubmit={handleSubmit}>
        <div className="template-form-content">
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
              <button type="button" className="btn btn-secondary" onClick={() => setActiveModal("picker")}>
                + Dodaj ćwiczenie
              </button>
            </div>
            <div className="template-exercises">
                {selectedExercises.map((selectedExercise) => {
                    return (
                <div className="card" key={selectedExercise.exerciseId}>
                  <div className="card-row">
                  <div className="exercise-name">{selectedExercise.exerciseName}</div>                     
                    <button
                      type="button"
                      className="btn-remove"
                      aria-label={`Usuń ${selectedExercise.exerciseName} z planu`}
                      onClick={() => removeExercise(selectedExercise.exerciseId)}
                    >
                      ✕
                    </button>
                  </div>
                  <div className="card-row">
                    <button
                      type="button"
                      className="btn-set-control"
                      disabled={selectedExercise.plannedSets <= 1}
                      onClick={(e) => {
                        e.stopPropagation();
                        changeSets(selectedExercise.exerciseId, -1);
                      }}
                    >
                      -
                    </button>
                    <span>Serie: {selectedExercise.plannedSets}</span>
                    <button
                      type="button"
                      className="btn-set-control"
                      onClick={(e) => {
                        e.stopPropagation();
                        changeSets(selectedExercise.exerciseId, 1);
                      }}
                    >
                      +
                    </button>
                  </div>
                               
              </div>
                    )})}
            </div>
        </div>
        <div className="template-form-actions">
          <button type="submit" className="btn btn-primary">
            Zapisz plan
          </button>  
        </div>   
      </form>
           
        <ExercisePickerModal
         isOpen={activeModal=="picker"}
         onClose={()=>setActiveModal("none")}
         onSelect={(exercise)=>
         {
           setSelectedExercises(current => {
              if (current.some(x => x.exerciseId === exercise.id)) {
                return current;
              }

              return [
                ...current,
                {
                  exerciseId: exercise.id,
                  exerciseName: exercise.name,
                  plannedSets: 3,
                  order: current.length + 1,
                },
              ];
            });
         }}
         onCreateNew={()=>{
          setActiveModal("create")
         }}
        />

        <CreateExerciseModal
        isOpen={activeModal=="create"}
        onClose={() => setActiveModal("none")}
        onCreated={(exercise) => {
          setSelectedExercises((current) => [
                ...current,
                {
                    exerciseId: exercise.id,
                    exerciseName: exercise.name,
                    plannedSets: 3, 
                    order: current.length + 1,
                },
            ]);
            setActiveModal("none");
        }}
        
      />
    </section>
)
}