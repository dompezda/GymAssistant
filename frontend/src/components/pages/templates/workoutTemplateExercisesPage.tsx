import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";

type Exercise = {
  id: string;
  name: string;
  muscleGroup: number | null;
  muscleGroupLabel: string | null;
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
  const state = location.state as { title?: string; notes?: string } | null;
  const [searchText, setSearchText] = useState("");
  const [selectedExercises, setSelectedExercises] = useState<
    SelectedExercise[]
  >([]);
  useEffect(() => {
    if (!state?.title) {
      navigate("/workout-templates/create");
      return;
    }

    async function loadExercises() {
      const response = await fetch("http://localhost:5235/api/exercises");

      if (!response.ok) {
        const error = await response.text();
        console.error(error);
        return;
      }

      const data = await response.json();
      setExercises(data);
    }

    loadExercises();
  }, [state?.title, navigate]);

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

    const response = await fetch(
      "http://localhost:5235/api/workout-templates",
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
    <section>
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

                {exercise.muscleGroupLabel && (
                  <p>{exercise.muscleGroupLabel}</p>
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
    </section>
  );
}