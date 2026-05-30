import { BrowserRouter, Routes, Route } from "react-router-dom";
import MainMenu from "./components/mainMenu/mainMenu";
import ExercisesPage from "./components/pages/exercisesPage";
import SettingsPage from "./components/pages/settingsPage";
import CreateExercisePage from "./components/pages/createExercisePage";
import WorkoutsTemplatesPage from "./components/pages/workoutsTemplatesPage";


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
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;