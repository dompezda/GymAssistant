import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

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
      const response = await fetch("http://localhost:5235/api/workout-templates");

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
    <section>
      <button
        className="btn btn-secondary"
        onClick={() => navigate("/workout-sessions/start")}
      >
        ← Wróć
      </button>

      <h1>Wybierz plan treningowy</h1>
      <p>Wybierz plan, z którego chcesz rozpocząć sesję.</p>

      <div className="stack">
        {templates.map((template) => (
          <article
            key={template.id}
            className="card"
            onClick={() =>
              navigate(`/workout-sessions/setup/from-template/${template.id}`)
            }
          >
            <h2>{template.title}</h2>

            {template.notes && <p>{template.notes}</p>}
          </article>
        ))}
      </div>
    </section>
  );
}