import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

type MuscleGroupOption = {
  value: number;
  label: string;
};

export default function CreateExercisePage() {
    
  const navigate = useNavigate();    
  const [name, setName] = useState("");
  const [selectedMuscleGroups, setSelectedMuscleGroups] = useState<number[]>([]);
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

  function toggleMuscleGroup(value: number) {
    setSelectedMuscleGroups((prev) =>
      prev.includes(value)
        ? prev.filter((x) => x !== value)
        : [...prev, value]
    );
  }

 async function handleSubmit(event: React.FormEvent) {
  event.preventDefault();
  setError(null);

  if (selectedMuscleGroups.length === 0) {
    setError("Nie udało się dodać ćwiczenia. Wybierz przynajmniej jedną grupę mięśniową.");
    return;
  }

  const response = await fetch("http://localhost:5235/api/exercises", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      name: name.trim(),
      muscleGroups: selectedMuscleGroups,
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
    className="form-field"
      type="text"
      value={name}
      onChange={(e) => setName(e.target.value)}
      required
    />
  </label>
  </div>
  <div>
  <p>Grupy mięśniowe</p>

  <div className="checkbox-grid">
    {muscleGroups.map((group) => (
      <label key={group.value} className="checkbox-item">
        <input
          type="checkbox"
          checked={selectedMuscleGroups.includes(group.value)}
          onChange={() => toggleMuscleGroup(group.value)}
        />
        {group.label}
      </label>
    ))}
  </div>
</div>
  <button className="btn btn-primary" type="submit">
    Zapisz
  </button>
</form>
            
  </section>
  );
}