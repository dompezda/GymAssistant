import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";

type WorkoutSession = {
id:string;
notes: string | null
startedAt: string;
finishedAt: string | null;
exerciseCount: number;
setCount:number;
totalVolume: number;
durationMinutes:number | null;
}

export default function WorkoutSessionsPage(){
const navigate = useNavigate();
    const[workoutSessions, setWorkoutSessions] = useState<WorkoutSession[]>([])
    
useEffect(()=>
{

    async function fetchSessions(): Promise<WorkoutSession[]> {
        const response = await fetchApi("http://localhost:5235/api/workout-sessions");
        if (!response.ok) {
            const error = await response.text();
            console.error(error);
            return [];
        }
        return await response.json();
    }
    async function loadSessions() {
        const data = await fetchSessions();
        setWorkoutSessions(data);
    }

loadSessions();

},[])



    return(
        <section className="page stack">

        <button className="btn btn-secondary" onClick={() => navigate("/")}>
            ← Wróć
        </button>

      <header>
        <h1>Historia treningów</h1>
      </header>

       
        {workoutSessions.length === 0 && (
            <div className="card">
                <h2>Brak historii treningów</h2>
                <p>Nie masz jeszcze żadnego zakończonego treningu.</p>
            </div>
            )}
        {workoutSessions.map((elem)=>
        <article key={elem.id} className="card" onClick={()=> navigate(`/workout-sessions/${elem.id}/summary`)}>
            <h2>Trening z dnia {new Date(elem.startedAt).toLocaleString("pl-PL")}</h2>
            {elem.notes ?? <p>{elem.notes}</p>}
            <p> Ilość setów {elem.setCount}</p>
            <p>Ilość ćwiczeń {elem.exerciseCount}</p>
            <p>Volume {elem.totalVolume}</p>
            <p>Czas trwania {elem.durationMinutes} min</p>
          </article>
        

        
        )}            

        </section>
    );
}


