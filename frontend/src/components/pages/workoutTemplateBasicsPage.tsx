import { useState } from "react";
import { useNavigate } from "react-router-dom";

export default function WorkoutTemplateBasicsPage() {
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [notes, setNotes] = useState("");

  async function HandleSubmit(event: React.FormEvent) {
    event.preventDefault();
      navigate("/workout-templates/exercises", {
        state: {
            title,
            notes,
        },
    });
  }

  return (
    <section>
      <button
        className="btn btn-secondary"
        onClick={() => navigate("/workout-templates")}
      >
        ← Wróć
      </button>
      <h1>Tworzenie planu treningowego</h1>
      <form onSubmit={HandleSubmit} className="stack">
        <div>
          <label>
            Tytuł
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
            />
          </label>
        </div>
        <div>
          <label>
            Notatki
            <textarea
              name="notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            ></textarea>
          </label>
        </div>

        <button type="submit" className="btn btn-primary" onClick={() => navigate("/workout-templates/exercises")}>
          Dalej
        </button>
      </form>
    </section>
  );
}
