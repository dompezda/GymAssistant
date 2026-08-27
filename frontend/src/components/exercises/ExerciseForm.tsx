import {useEffect, useState} from "react";
import { fetchApi } from "../../api/fetchApi";


type ExerciseFormValues = {
  name: string;
  muscleGroups: number[];
};

type MuscleGroupOption = {
  value: number;
  label: string;
};
type ExerciseFormProps = {
  onClose: () => void;
  onSubmit: (values: ExerciseFormValues) => void | Promise<void>;
  title: string;
  buttonValue: string;
  initialValues?: ExerciseFormValues;
  readOnly?:boolean
};

export default function ExerciseForm({
  onClose,
  onSubmit,
  title,
  buttonValue,
  initialValues,
  readOnly
}: ExerciseFormProps) {


  const values = initialValues ?? {
      name: "",
      muscleGroups: []
    };

  const [muscleGroups, setMuscleGroups] = useState<MuscleGroupOption[]>([]);
  const [name, setName] = useState(values.name);
  const [selectedMuscleGroups, setSelectedMuscleGroups] = useState<number[]>(values.muscleGroups);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    async function loadMuscleGroups() {
      const response = await fetchApi("/api/muscle-groups");
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

  if (!name.trim()) {
    setError("Podaj nazwę ćwiczenia.");
    return;
  }

  if (selectedMuscleGroups.length === 0) {
    setError("Wybierz przynajmniej jedną grupę mięśniową.");
    return;
  }
  const result: ExerciseFormValues ={
    name: name.trim(),
    muscleGroups: selectedMuscleGroups
  };
  await onSubmit(result);
}



return(
<div>
      <div className="form-title">
        <h2>{title}</h2>
        {readOnly && <span className="system-badge">System</span>}
      </div>

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
            disabled = {readOnly}
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
        disabled = {readOnly}
      />
      {group.label}
    </label>
  ))}
</div>
        {!readOnly &&
          <button className="btn btn-primary" type="submit" >
            {buttonValue}
          </button>
        }
        <button className="btn btn-secondary" type="button" onClick={onClose}>
          Zamknij
        </button>
      </form>
    </div>
)}