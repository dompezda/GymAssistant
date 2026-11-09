import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";


type SelectedExercise = {
  exerciseId: string;
  exerciseName: string;
  plannedSets: number;
};

export default function WorkoutTemplateBasicsPage() {

  const location = useLocation();

  const state = location.state as {
    title?: string;
    notes?: string;
    addedExercises?: SelectedExercise[];
  } | null;
  const navigate = useNavigate();
  const [title, setTitle] = useState(state?.title ?? "");
  const [notes, setNotes] = useState(state?.notes ?? "");
  

  async function HandleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!title.trim()) return;
      navigate("/workout-templates/exercises", {
        state: {
            title: title.trim(),
            notes,
            addedExercises: state?.addedExercises ?? [],
        },
    });
  } 

  return (
    <section className="page">
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

        <button type="submit" className="btn btn-primary">
          Dalej
        </button>
      </form>
    </section>
  );
}
