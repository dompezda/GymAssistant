import { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";
import "./workoutTemplatesPage.css";
type Template={
  id: string;
  title: string;
  notes: string | null;
}

export default function WorkoutsTemplatesPage() {
  const navigate = useNavigate();
  const [templates, setTemplates] = useState<Template[]>([]);
  useEffect(() => {
    async function loadTemplates() {
      const response = await fetchApi("/api/workout-templates");

        if (!response.ok) {
          const error = await response.text();
          console.error(error);
          return;
        }
      const data = await response.json();
      setTemplates(data);
    }

    loadTemplates();
  }, []);

  return (
    <section className="page stack templates-page">
      <div className="page-header">
      <button className="btn back-button" onClick={() => navigate("/")}>
        ← Wróć
      </button>

      <header>
        <h1 className="page-title">Plany treningowe</h1>
      </header>
      </div>
      <button className="btn btn-primary" onClick={() => navigate("/workout-templates/create")}>
        + Dodaj plan treningowy
      </button>
        <div className="stack">
        {templates.map((template) => (
         <Link
            key={template.id}
            className="card template-card"
            to={`/workout-templates/${template.id}`}>
            <h2 className="card-title">{template.title}</h2>
            {template.notes && (
              <p className="card-text">{template.notes}</p>
            )}
          </Link>
        ))}
      </div>


    </section>
  );
}