import {useState} from "react";
import ExerciseForm from "./ExerciseForm"
import { fetchApi } from "../../api/fetchApi";

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

type ExerciseFormValues = {
  name: string;
  muscleGroups: number[];
};

export default function CreateExerciseModal({
  isOpen,
  onClose,
  onCreated,
}: CreateExerciseModalProps) {
  const [error, setError] = useState<string | null>(null);


  
async function createExercise(exercise : ExerciseFormValues){
  const response = await fetchApi("/api/exercises", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      name: exercise.name.trim(),
      muscleGroups: exercise.muscleGroups
    }),
  });

  if (!response.ok) {
    setError("Błąd dodawania ćwiczenia.");
    return;
  }
  const result: Exercise = await response.json();
  console.log("Created exercise with ID:", result.id);
  onCreated(result);
}

if(!isOpen)
  return null;
 
  return (
    <section>
      <div className="modal-backdrop">
    <div className="modal">
    <ExerciseForm
    title = "Utwórz nowe ćwiczenie"
    buttonValue = "Dodaj ćwiczenie"
    onClose = {onClose}
    onSubmit={async (exercise : ExerciseFormValues)=>{
      await createExercise(exercise)
    }}

    />
    <div> {error}</div>
    </div>
    </div>
    </section>
);
}