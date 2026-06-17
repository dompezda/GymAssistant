import {useEffect, useState} from "react";

type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};

type CreateExerciseModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onCreated: (exercise: Exercise) => void;
};

type MuscleGroupOption = {
  value: number;
  label: string;
};

export default function CreateExerciseModal({
  isOpen,
  onClose,
  onCreated,
}: CreateExerciseModalProps) {

  const [muscleGroups, setMuscleGroups] = useState<MuscleGroupOption[]>([]);
  const [name, setName] = useState("");
  const [selectedMuscleGroups, setSelectedMuscleGroups] = useState<number[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadMuscleGroups() {
      const response = await fetch("http://localhost:5235/api/muscle-groups");
      const data = await response.json();
      setMuscleGroups(data);
    }

    loadMuscleGroups();
  }, []);
  if (!isOpen) {
    return null;
  }
  
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

  if (!name.trim()) {
    setError("Podaj nazwę ćwiczenia.");
    return;
  }

  if (selectedMuscleGroups.length === 0) {
    setError("Wybierz przynajmniej jedną grupę mięśniową.");
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

  setName("");
  setSelectedMuscleGroups([]);
  const result: Exercise = await response.json();
  console.log("Created exercise with ID:", result.id);
  onCreated(result);
}

 
  return (
  <div className="modal-backdrop">
    <div className="modal">
      <h2>Utwórz nowe ćwiczenie</h2>

      <form className="stack" onSubmit={handleSubmit}>
        {error && <p className="error">{error}</p>}

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

        <p>Grupy mięśniowe:</p>

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

        <button className="btn btn-primary" type="submit">
          Zapisz
        </button>

        <button className="btn btn-secondary" type="button" onClick={onClose}>
          Zamknij
        </button>
      </form>
    </div>
  </div>
);
}