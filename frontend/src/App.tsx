import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import MainMenu from "./components/mainMenu/mainMenu";
import ExercisesPage from "./components/pages/exercise/exercisesPage";
import SettingsPage from "./components/pages/settingsPage";
//import CreateExercisePage from "./components/pages/exercise/createExercisePage";

import WorkoutsTemplatesPage from "./components/pages/templates/workoutTemplatesPage";
import WorkoutTemplateBasicsPage from "./components/pages/templates/workoutTemplateBasicsPage";
import WorkoutTemplateExercisesPage from "./components/pages/templates/workoutTemplateExercisesPage";
import WorkoutTemplatePage from "./components/pages/templates/workoutTemplatePage";

import StartWorkoutPage from "./components/pages/session/startWorkoutPage";
import SelectWorkoutTemplatePage from "./components/pages/session/selectWorkoutTemplatePage";
import WorkoutSessionPage from "./components/pages/session/workoutSessionPage";
import WorkoutSessionSetupPage from "./components/pages/session/workoutSessionSetupPage";
import WorkoutSessionSummaryPage from "./components/pages/session/workoutSessionSummaryPage";
import WorkoutTemplateEdit from "./components/pages/templates/workoutTemplateEdit";
import WorkoutSessionsPage from "./components/pages/session/workoutSessionsPage";
import ExerciseHistory from "./components/pages/exercise/exerciseHistory";
import LoginPage from "./components/pages/loginPage";
import AuthProvider from "./auth/AuthProvider";
import { ProtectedRoute } from "./auth/protectedRoute";
import RegisterPage from "./components/pages/registerPage";

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>  
        <main className="app">
          <Routes>
            <Route path="/" element={<Navigate to="/main-menu" replace />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

          <Route element={<ProtectedRoute />}>
          
            <Route path="/main-menu" element={<MainMenu />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/exercises" element={<ExercisesPage />} />
            <Route path="/exercises/:id/history" element={<ExerciseHistory/>}/>
            {/* <Route path="/exercises/create" element={<CreateExercisePage />} /> */}

            <Route path="/workout-templates" element={<WorkoutsTemplatesPage />} />
            <Route path="/workout-templates/create" element={<WorkoutTemplateBasicsPage />} />
            <Route path="/workout-templates/exercises" element={<WorkoutTemplateExercisesPage />} />
            <Route path="/workout-templates/:id" element={<WorkoutTemplatePage />} />
            <Route path="/workout-templates/:id/edit" element={<WorkoutTemplateEdit />} />

            <Route path="/workout-sessions/start" element={<StartWorkoutPage />} />
            <Route path="/workout-sessions/select-template" element={<SelectWorkoutTemplatePage />} />
            <Route path="/workout-sessions/setup" element={<WorkoutSessionSetupPage />} />
            <Route path="/workout-sessions/setup/from-template/:templateId" element={<WorkoutSessionSetupPage />} />
            <Route path="/workout-sessions/:id" element={<WorkoutSessionPage />} />
            <Route path="/workout-sessions/:id/summary" element={<WorkoutSessionSummaryPage/>} />
            <Route path="/workout-sessions" element={<WorkoutSessionsPage/>}/>
          </Route>  
            <Route path="*" element={<div>Nie znaleziono strony</div>} />
          </Routes>
        </main>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;