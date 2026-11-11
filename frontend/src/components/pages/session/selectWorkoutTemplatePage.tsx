import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";

type Template = {
  id: string;
  title: string;
  notes: string | null;
};

export default function SelectWorkoutTemplatePage() {
  const navigate = useNavigate();
  const [templates, setTemplates] = useState<Template[]>([]);

  useEffect(() => {
    async function loadTemplates() {
      const response = await fetchApi("/api/workout-templates");

      if (!response.ok) {
        console.error(await response.text());
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
            <h1 className="page-title">Wybierz plan</h1>
          </header>
       </div>
      <div className="stack">
        <p>Wybierz plan, z którego chcesz rozpocząć sesję.</p>
        {templates.map((template) => (
          <Link
            key={template.id}
            className="card"
            to={`/workout-sessions/setup/from-template/${template.id}`}
          >
            <h2 className="card-title">{template.title}</h2>

            {template.notes && <p className="card-text">{template.notes}</p>}
          </Link>
        ))}
      </div>
    </section>
  );
}