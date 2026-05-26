import { BrowserRouter, Routes, Route } from "react-router-dom";
import MainMenu from "./components/mainMenu/mainMenu";
import ExercisesPage from "../src/components/pages/exercisesPage";
import WorkoutsPage from "../src/components/pages/workoutsPage";
import SettingsPage from "../src/components/pages/settingsPage";
import CreateExercisePage from "./components/pages/CreateExercisePage";

function App() {
  return (
    <BrowserRouter>
      <main className="app">
        <Routes>
          <Route path="/" element={<MainMenu />} />
          <Route path="/exercises" element={<ExercisesPage />} />
          <Route path="/workouts" element={<WorkoutsPage />} />
          <Route path="/settings" element={<SettingsPage />} />
          <Route path="/exercises/create" element={<CreateExercisePage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;