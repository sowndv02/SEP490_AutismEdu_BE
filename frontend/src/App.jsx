import { Typography } from "@mui/material"
import { BrowserRouter, Route, Routes } from "react-router-dom"
import routes from "./routes/Routes"


function App() {

  return (
    <>
      <BrowserRouter>
        <Routes>
          {routes.routes.map((route) => (
            <Route
              key={route.path}
              path={route.path}
              element={<route.element />}
            />
          ))}
        </Routes>
      </BrowserRouter>
    </>
  )
}

export default App
