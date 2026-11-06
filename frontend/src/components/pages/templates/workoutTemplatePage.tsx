import { useEffect, useState} from "react";
import { useNavigate, useParams } from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";
import "./workoutTemplatesPage.css";

type WorkoutTemplateResponseDto = {
  id: string;
  title: string;
  notes: string | null;
  exercises: WorkoutTemplateExerciseResponseDto[];
};
type WorkoutTemplateExerciseResponseDto = {
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
  const [isDescriptionExpanded, setIsDescriptionExpanded] = useState(false);

  useEffect(() => {
    if (!id) return;

    async function loadTemplate() {
      const response = await fetchApi(`/api/workout-templates/${id}`);

      if (!response.ok) {
        throw new Error("Nie udało się załadować planu treningowego.");
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
    <section className="page stack templates-page">
      <div className="page-header">
        <button className="btn back-button" onClick={() => navigate("/workout-templates")}>
          ← Wróć
        </button>

        <h1 className="page-title">{template.title}</h1>
      </div>

      {template.notes && <p
        onClick={() => setIsDescriptionExpanded(prev => !prev)}
        className={`template-description ${isDescriptionExpanded ? "expanded" : ""}`}
      >{template.notes}</p>}
      <button className="btn btn-primary" onClick={() => navigate(`/workout-templates/${template.id}/edit`)}>
        Edytuj
      </button>
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