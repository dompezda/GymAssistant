import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";

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
    <section className="page">
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>

      <h1>Plany treningowe</h1>
      <p>Tutaj będzie lista planów treningowych.</p>

        <div className="stack">
        {templates.map((template) => (
          <article key={template.id} className="card"
            onClick={() => navigate(`/workout-templates/${template.id}`)}>
            <h2>{template.title}</h2>

            {template.notes && (
              <p>{template.notes}</p>
            )}
            
          </article>
        ))}
      </div>

      <button className="btn btn-primary" onClick={() => navigate("/workout-templates/create")}>
        + Dodaj plan treningowy
      </button>
    </section>
  );
}