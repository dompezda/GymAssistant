import { useEffect, useState} from "react";
import { useNavigate, useParams } from "react-router-dom";

export type WorkoutTemplateResponseDto = {
  id: string;
  title: string;
  notes: string | null;
  exercises: WorkoutTemplateExerciseResponseDto[];
};

export type WorkoutTemplateExerciseResponseDto = {
  exerciseId: string;
  exerciseName: string;
  plannedSets: number;
  order: number;
};


export default function WorkoutTemplatePage() {
   const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [template, setTemplate] = useState<WorkoutTemplateResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!id) return;

    async function loadTemplate() {
      const response = await fetch(`http://localhost:5235/api/workout-templates/${id}`);

      if (!response.ok) {
        throw new Error("Failed to load workout template");
      }

      const data: WorkoutTemplateResponseDto = await response.json();
      setTemplate(data);
      setIsLoading(false);
    }

    loadTemplate();
  }, [id]);

  if (isLoading) {
    return <p>Ładowanie...</p>;
  }

  if (!template) {
    return <p>Nie znaleziono planu treningowego.</p>;
  }


    return (
    <section className="page">
      <button className="btn btn-secondary" onClick={() => navigate("/workout-templates")}>
        ← Wróć
      </button>

      <h1>{template.title}</h1>

      {template.notes && <p>{template.notes}</p>}

      <div className="stack">
        {template.exercises.map((exercise) => (
          <article key={exercise.exerciseId} className="card">
            <h2>{exercise.exerciseName}</h2>
            <p>Liczba serii: {exercise.plannedSets}</p>
          </article>
        ))}
      </div>
    </section>
  );
}