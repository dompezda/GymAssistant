import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
type Exercise = {
  id: string;
  name: string;
  muscleGroup: number | null;
  muscleGroupLabel: string | null;
  isSystem: boolean;
};

export default function ExercisesPage() {
    const navigate = useNavigate();

    const [exercises, setExercises] = useState<Exercise[]>([]);
    useEffect(() => {
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
  }, []);
  

  return (
    <section className="page stack">
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć1
      </button>

      <header className="page-header">
        <h1>Ćwiczenia</h1>
      </header>

      <div className="stack">
        {exercises.map((exercise) => (
          <article key={exercise.id} className="card">
            <h2>{exercise.name}</h2>

            {exercise.muscleGroupLabel && (
              <p>{exercise.muscleGroupLabel}</p>
            )}
          </article>
        ))}
      </div>

      <button className="btn btn-primary" onClick={() => navigate("/exercises/create")}>
        + Dodaj ćwiczenie
      </button>
    </section>
  );
}