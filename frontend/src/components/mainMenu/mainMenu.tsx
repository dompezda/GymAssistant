import { useNavigate } from 'react-router-dom';
import '../../styles/global.css';


export default function MainMenu() {
  const navigate = useNavigate();

  async function checkMe(): Promise<void> {
    const accessToken = localStorage.getItem("accessToken");

    if (!accessToken) {
        console.error("Access token is missing");
        return;
    }
    const response = await fetch("http://localhost:5235/api/auth/me", {
        headers: {
            Authorization: `Bearer ${accessToken}`,
        },
    });

    if (!response.ok) {
        console.error("Check me failed:", response.status);
        return;
    }

    const user = await response.json();
    console.log(user);
}

  return (
    <section className="page main-menu">
      <h1>Gym Assistant</h1>

      <button className="btn btn-primary" onClick={() => navigate("/exercises")}>Ćwiczenia</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-templates")}>Plany Treningowe</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions/start")}>Rozpocznij trening</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions")}>Historia treningów</button>
      <button className="btn btn-secondary" onClick={() => navigate("/settings")}>Ustawienia</button>
      <button className="btn btn-secondary" onClick={() => checkMe()}>Check Me</button>
    </section>
  );
}