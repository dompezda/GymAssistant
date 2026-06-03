import { BrowserRouter, Routes, Route } from "react-router-dom";
import MainMenu from "./components/mainMenu/mainMenu";
import ExercisesPage from "./components/pages/exercisesPage";
import SettingsPage from "./components/pages/settingsPage";
import CreateExercisePage from "./components/pages/createExercisePage";
import WorkoutsTemplatesPage from "./components/pages/workoutsTemplatesPage";
import WorkoutTemplateBasicsPage from "./components/pages/workoutTemplateBasicsPage";
import WorkoutTemplateExercisesPage from "./components/pages/workoutTemplateExercisesPage";
import WorkoutTemplatePage from "./components/pages/workoutTemplatePage";


function App() {
  return (
    <BrowserRouter>
      <main className="app">
        <Routes>
          <Route path="/" element={<MainMenu />} />
          <Route path="/exercises" element={<ExercisesPage />} />
          <Route path="/workout-templates" element={<WorkoutsTemplatesPage />} />
          <Route path="/settings" element={<SettingsPage />} />
          <Route path="/exercises/create" element={<CreateExercisePage />} />
          <Route path="/workout-templates/create" element={<WorkoutTemplateBasicsPage />} />
          <Route path="/workout-templates/exercises" element={<WorkoutTemplateExercisesPage />} />
          <Route path="/workout-templates/:id" element={<WorkoutTemplatePage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;