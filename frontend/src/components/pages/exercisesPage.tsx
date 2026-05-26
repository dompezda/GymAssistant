import { useNavigate } from "react-router-dom";
type Exercise = {
  id: string;
  name: string;
  muscleGroup?: string | null;
};


export default function ExercisesPage() {
    const navigate = useNavigate();

    const exercises: Exercise[] = [];


  return (
    <section className="page stack">
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>

      <header className="page-header">
        <h1>Ćwiczenia</h1>
        <p>Lista ćwiczeń dostępnych do budowania treningów.</p>
      </header>

      <div className="stack">
        {exercises.map((exercise) => (
          <article key={exercise.id} className="card">
            <h2>{exercise.name}</h2>

            {exercise.muscleGroup && (
              <p>{exercise.muscleGroup}</p>
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