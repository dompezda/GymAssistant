import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import CreateExerciseModal from "../../exercises/CreateExerciseModal";
import { fetchApi } from "../../../api/fetchApi";
type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};
type SelectedExercise = {
  exerciseId: string;
  plannedSets: number;
  order: number;
};

export default function WorkoutTemplateExercisesPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [exercises, setExercises] = useState<Exercise[]>([]);
  const [isCreateExerciseModalOpen, setIsCreateExerciseModalOpen] = useState(false);
  const state = location.state as { title?: string; notes?: string } | null;
  const [searchText, setSearchText] = useState("");
  const [selectedExercises, setSelectedExercises] = useState<
    SelectedExercise[]
  >([]);

  async function fetchExercises(): Promise<Exercise[]> {
    const response = await fetchApi("/api/exercises");

      if (!response.ok) {
        const error = await response.text();
        console.error(error);
        return [];
      }

      const data = await response.json();
      return data;
  }

  useEffect(() => {
    if (!state?.title) {
      navigate("/workout-templates/create");
      return;
    }
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
},[state?.title, navigate]);

  if (!state?.title) {
    return null;
  }

  const visibleExercises = exercises.filter((exercise) =>
    exercise.name.toLowerCase().includes(searchText.trim().toLowerCase())
  );

  function getSelectedExercise(exerciseId: string) {
    return selectedExercises.find((x) => x.exerciseId === exerciseId);
  }
  function addExercise(exerciseId: string) {
    setSelectedExercises((current) => [
      ...current,
      {
        exerciseId,
        plannedSets: 3,
        order: current.length + 1,
      },
    ]);
  }
  function changeSets(exerciseId: string, delta: number) {
    setSelectedExercises((current) =>
      current.map((x) =>
        x.exerciseId === exerciseId
          ? { ...x, plannedSets: Math.max(1, x.plannedSets + delta) }
          : x
      )
    );
  }

  function removeExercise(exerciseId: string) {
    setSelectedExercises((current) =>
      current.filter((x) => x.exerciseId !== exerciseId)
    );
  }
  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();

    if (!state?.title) return;

    if (selectedExercises.length === 0) {
      alert("Dodaj przynajmniej jedno ćwiczenie.");
      return;
    }

    const response = await fetchApi("/api/workout-templates",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title: state.title,
          notes: state.notes,
          exercises: selectedExercises.map((x) => ({
            exerciseId: x.exerciseId,
            plannedSets: x.plannedSets,
            order: x.order,
          })),
        }),
      }
    );

    if (!response.ok) {
      const error = await response.text();
      console.error(error);
      return;
    }

    navigate("/workout-templates");
  }

  return (
    <section className="page">
      <form onSubmit={handleSubmit} className="stack">
        <button
          type="button"
          className="btn btn-secondary"
          onClick={() => navigate("/workout-templates/create")}
        >
          ← Wróć
        </button>

        <h1>{state.title}</h1>
        {state.notes && <p>{state.notes}</p>}

        <button
          type="button"
          className="btn btn-primary"
          onClick={() => setIsCreateExerciseModalOpen(true)}
        >
          Utwórz nowe ćwiczenie
        </button>
        <input
          type="text"
          placeholder="Szukaj ćwiczenia..."
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />

        <div className="stack">
          {visibleExercises.map((exercise) => {
            const selected = getSelectedExercise(exercise.id);

            return (
              <article
                key={exercise.id}
                className={`card ${selected ? "card-selected" : ""}`}
                onClick={() =>
                  selected
                    ? removeExercise(exercise.id)
                    : addExercise(exercise.id)
                }
              >
                <h2>{exercise.name}</h2>

                {exercise.muscleGroupLabels.length > 0 && (
                  <p>{exercise.muscleGroupLabels.join(", ")}</p>
                )}

                {selected && (
                  <div className="sets-row">
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        changeSets(exercise.id, -1);
                      }}
                    >
                      -
                    </button>

                    <span>Serie: {selected.plannedSets}</span>

                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        changeSets(exercise.id, 1);
                      }}
                    >
                      +
                    </button>
                  </div>
                )}
              </article>
            );
          })}
        </div>

        <button type="submit" className="btn btn-primary">
          Zapisz plan
        </button>
      </form>

      <CreateExerciseModal
        isOpen={isCreateExerciseModalOpen}
        onClose={() => setIsCreateExerciseModalOpen(false)}
        onCreated={async (createdExercise) => {
          const data = await fetchExercises();
          setExercises(data);
          addExercise(createdExercise.id);
          setIsCreateExerciseModalOpen(false);
        }}
      />
    </section>
  );
}