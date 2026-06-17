import { useEffect, useState } from "react";

type Exercise = {
  id: string;
  name: string;
  muscleGroups: number[];
  muscleGroupLabels: string[];
  isSystem: boolean;
};


type ExercisePickerModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (exercise: Exercise) => void;
  onCreateNew: () => void;
};

export default function ExercisePickerModal({
    isOpen,
    onClose,
    onSelect,
    onCreateNew
}: ExercisePickerModalProps) {
    const [exercises, setExercises] = useState<Exercise[]>([]);
    useEffect(() => {
        async function loadExercises() {
            const response = await fetch("http://localhost:5235/api/exercises");
            const data = await response.json();
            setExercises(data);
        }
        loadExercises();
    }, []);
      if (!isOpen) {
        return null;
       }


return (
  <>
    <div className="modal-backdrop">
      <div className="modal modal-scrollable">
        <div className="modal-header">
          <h3>Wybierz ćwiczenie</h3>

          <button
            type="button"
            className="btn btn-primary"
            onClick={() => onCreateNew()}
          >
            + Dodaj nowe ćwiczenie
          </button>
        </div>

        <div className="modal-body">
          {exercises.map((exercise) => (
            <button
              key={exercise.id}
              type="button"
              className="btn btn-secondary"
              onClick={() => {
                onSelect(exercise);
                onClose();
              }}
            >
              {exercise.name}
            </button>
          ))}
        </div>

        <div className="modal-footer">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={onClose}
          >
            Zamknij
          </button>
        </div>
      </div>
    </div>
  </>
)};