import { useEffect, useState } from "react";
import ExerciseForm from "./ExerciseForm";
import { useNavigate } from "react-router-dom";
import { fetchApi } from "../../api/fetchApi";

type ExerciseFormValues = {
  name: string;
  muscleGroups: number[];
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
        muscleGroups: exercise.muscleGroups
    }

    async function editExercise(exerciseValues:ExerciseFormValues) {
        const response = await fetchApi(`/api/exercises/${exercise.id}`, {
            method: "PATCH",
            headers: {
            "Content-Type": "application/json",
            },
            body: JSON.stringify({
                name: exerciseValues.name.trim(),
                muscleGroups: exerciseValues.muscleGroups
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

    useEffect(()=>{
      if(!isOpen) return;
    
      const prev = document.body.style.overflowY;
      document.body.style.overflowY = "hidden";
    
      return ()=>
      {
        document.body.style.overflowY = prev;
      }
      
    },[isOpen])
    

    if(!isOpen)
        return null;

    async function checkHistory()
    {
        navigate(`/exercises/${exercise.id}/history`)
    };
    return(
    <section>
<div className="modal-backdrop">
    <div className="modal">
        <ExerciseForm
            title={exercise.isSystem ? "Podgląd ćwiczenia" :"Edytuj ćwiczenie"}
            buttonValue="Edytuj"
            initialValues={initialValue}
            onHistoryClick={checkHistory}
            onClose = {onClose}
            readOnly = {exercise.isSystem}
            onSubmit={async (exerciseValues : ExerciseFormValues)=>{
                await editExercise(exerciseValues)
            
            }}
        />
        <div>{error}</div>
        </div>
        </div>
    </section>
)
}