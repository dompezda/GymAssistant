import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import EditExerciseModal from "../../exercises/EditExerciseModal";
import { fetchApi } from "../../../api/fetchApi";
type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};

export default function ExercisesPage() {
    const navigate = useNavigate();
    const [exercises, setExercises] = useState<Exercise[]>([]);
    const [isCreateExerciseModalOpen, setIsCreateExerciseModalOpen] = useState(false);
    const [searchText, setSearchText] = useState("");
    const [selectedExercise, setSelectedExercise] = useState<Exercise | null>(null);
    
async function fetchExercises(): Promise<Exercise[]> {
  const response = await fetchApi("http://localhost:5235/api/exercises");

  if (!response.ok) {
    const error = await response.text();
    console.error(error);
    return [];
  }

  return await response.json();
}
async function loadExercises() {
  const data = await fetchExercises();
  setExercises(data);
}

useEffect(() => {
  let ignore = false;

  async function loadInitialExercises() {
    const data = await fetchExercises();

    if (!ignore) {
      setExercises(data);
    }
  }

  loadInitialExercises();

  return () => {
    ignore = true;
  };
}, []);
    const visibleExercises = exercises.filter((exercise) =>
    exercise.name.toLowerCase().includes(searchText.trim().toLowerCase())
  );

  return (
    <section className="page stack">
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>

      <header>
        <h1>Ćwiczenia</h1>
      </header>
      <button className="btn btn-primary" onClick={() => setIsCreateExerciseModalOpen(true)}>
        + Dodaj ćwiczenie
      </button>
        <input
          type="text"
          placeholder="Szukaj ćwiczenia..."
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />

      <div className="stack">
        {visibleExercises.map((exercise) => (
          <article key={exercise.id} className="card" onClick={() =>setSelectedExercise(exercise)}>
            <h2>{exercise.name}</h2>

            {exercise.muscleGroupLabels?.length > 0 && (
              <p>{exercise.muscleGroupLabels.join(", ")}</p>
            )}
          </article>
        ))}
      </div>

      <CreateExerciseModal
        isOpen={isCreateExerciseModalOpen}
        onClose={() => setIsCreateExerciseModalOpen(false)}
        onCreated={() => {
          loadExercises();
          setIsCreateExerciseModalOpen(false);
        }}
      />
      {selectedExercise !== null ?
      <EditExerciseModal
        isOpen={selectedExercise !== null}
        onClose={() => setSelectedExercise(null)}
        exercise={selectedExercise}
        onUpdated={()=>
          {
            loadExercises();
            setSelectedExercise(null);
          }    
        }
      />
      : null}
    </section>
    
  );
}