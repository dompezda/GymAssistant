import { useEffect, useState } from "react";
import { fetchApi } from "../../api/fetchApi";

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
            const response = await fetchApi("/api/exercises");
            const data = await response.json();
            setExercises(data);
        }
        loadExercises();
    }, [isOpen]);
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
              className="btn btn-secondary exercise-picker-button"
              onClick={() => {
                onSelect(exercise);
                onClose();
              }}
            >
              <span className="exercise-name">{exercise.name}</span>

                {exercise.isSystem && (
                  <span className="exercise-badge">System</span>
                )}
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