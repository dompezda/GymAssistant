import { useState } from "react";
import ExerciseForm from "./ExerciseForm";
import { useNavigate } from "react-router-dom";
import { fetchApi } from "../../api/fetchApi";

type ExerciseFormValues = {
  name: string;
  muscleGroups: number[];
  isSystem: boolean;
};
type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};

type EditExerciseModalProps =
{
    isOpen : boolean;
    onClose : ()=> void;
    onUpdated : () => void;
    exercise : Exercise
}

export default function EditExerciseModal({
    isOpen,
    onClose,
    onUpdated,
    exercise
}: EditExerciseModalProps) { 
    const navigate = useNavigate();
    const [error, setError] = useState<string | null>(null);
    const initialValue : ExerciseFormValues = {
        name : exercise.name,
        muscleGroups: exercise.muscleGroups,
        isSystem: exercise.isSystem
        
    }

    async function editExercise(exerciseValues:ExerciseFormValues) {
        const response = await fetchApi(`/api/exercises/${exercise.id}`, {
            method: "PATCH",
            headers: {
            "Content-Type": "application/json",
            },
            body: JSON.stringify({
                name: exerciseValues.name.trim(),
                muscleGroups: exerciseValues.muscleGroups,
                isSystem: exerciseValues.isSystem
            }),
        });

        if (!response.ok) {
            setError("Błąd edycji ćwiczenia.");
            return;
        }
        const result = await response.json();
        console.log("Edited exercise with ID:", result);
        onUpdated();
    }

    if(!isOpen)
        return null;
    if (!exercise) {
        return <div>Ładowanie...</div>;
    }
    return(
    <section>
<div className="modal-backdrop">
    <div className="modal">
        <ExerciseForm
            title="Edytuj ćwiczenie"
            buttonValue="Edytuj"
            initialValues={initialValue}
            onClose = {onClose}
            onSubmit={async (exerciseValues : ExerciseFormValues)=>{
                await editExercise(exerciseValues)
            }}
        />
        <button className="btn btn-secondary" onClick={()=>navigate(`/exercises/${exercise.id}/history`)}>
            Historia ćwiczenia
        </button>
        <div>{error}</div>
        </div>

        </div>
    </section>
)
}