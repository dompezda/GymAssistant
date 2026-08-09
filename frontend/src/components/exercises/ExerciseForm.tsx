import {useEffect, useState} from "react";
import { fetchApi } from "../../api/fetchApi";


type ExerciseFormValues = {
  name: string;
  muscleGroups: number[];
  isSystem: boolean;
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
  initialValues?: ExerciseFormValues
};

export default function ExerciseForm({
  onClose,
  onSubmit,
  title,
  buttonValue,
  initialValues
}: ExerciseFormProps) {


  const values = initialValues ?? {
      name: "",
      muscleGroups: [],
      isSystem: false,
    };

  const [muscleGroups, setMuscleGroups] = useState<MuscleGroupOption[]>([]);
  const [name, setName] = useState(values.name);
  const [selectedMuscleGroups, setSelectedMuscleGroups] = useState<number[]>(values.muscleGroups);
  const [isSystem,setIsSystem] = useState(values.isSystem);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadMuscleGroups() {
      const response = await fetchApi("http://localhost:5235/api/muscle-groups");
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
    muscleGroups: selectedMuscleGroups,
    isSystem: isSystem
  };
  await onSubmit(result);
}



return(
<div>
      <h2>{title}</h2>

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
        <div className="checkbox-item">
          <input
            type="checkbox"
            checked={isSystem}
            onChange={(e) => setIsSystem(e.target.checked)}
            />
          <label> isSystem</label>

        </div>

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
          {buttonValue}
        </button>
        <button className="btn btn-secondary" type="button" onClick={onClose}>
          Zamknij
        </button>
      </form>
    </div>
)}