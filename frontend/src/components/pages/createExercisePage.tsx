import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

type MuscleGroupOption = {
  value: number;
  label: string;
};

export default function CreateExercisePage() {
    
  const navigate = useNavigate();    
  const [name, setName] = useState("");
  const [muscleGroup, setMuscleGroup] = useState("");
  const [muscleGroups, setMuscleGroups] = useState<MuscleGroupOption[]>([]);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    async function loadMuscleGroups() {
      const response = await fetch("http://localhost:5235/api/muscle-groups");
      const data = await response.json();
      setMuscleGroups(data);
    }

    loadMuscleGroups();
  }, []);

 async function handleSubmit(event: React.FormEvent) {
  event.preventDefault();
  setError(null);

  if (muscleGroup === "" || isNaN(Number(muscleGroup))) {
    setError("Nie udało się dodać ćwiczenia. Nieprawidłowa grupa mięśniowa.");
    return;
  }

  const response = await fetch("http://localhost:5235/api/exercises", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      name: name.trim(),
      muscleGroup: Number(muscleGroup),
    }),
  });

  if (!response.ok) {
    setError("Błąd dodawania ćwiczenia.");
    return;
  }

  navigate("/exercises");
}
    return (
        <section className="page stack">
            <button className="btn btn-secondary" onClick={() => navigate("/exercises")}>
                ← Wróć
            </button>
            <form className="stack" onSubmit={handleSubmit}>
  <div>
    <div>
      {error && <p className="error">{error}</p>}
    </div>
 <label>
    Nazwa ćwiczenia
    <input
      type="text"
      value={name}
      onChange={(e) => setName(e.target.value)}
      required
    />
  </label>
  </div>
  <div>
 <label>
    Grupa mięśniowa
    <select
      value={muscleGroup}
      onChange={(e) => setMuscleGroup(e.target.value)}
    >
      <option value="">Brak</option>

      {muscleGroups.map((group) => (
        <option key={group.value} value={group.value}>
          {group.label}
        </option>
      ))}
    </select>
  </label>
  </div>
  <button className="btn btn-primary" type="submit">
    Zapisz
  </button>
</form>
            
        </section>
  );
}