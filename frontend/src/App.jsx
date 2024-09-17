import { BrowserRouter, Route, Routes } from "react-router-dom";
import AdminLayout from "./layout/AdminLayout";
import { adminRoutes, publicRoutes } from "./routes/Routes";
import ClientLayout from "./layout/ClientLayout";

function App() {
  return (
    <>
      <BrowserRouter>
        <Routes>
          <Route path="/autismedu" element={<ClientLayout />}>
            {publicRoutes.map((route) => (
              <Route
                key={route.path}
                path={`/autismedu/${route.path}`}
                element={<route.element />}
              />
            ))}
          </Route>
          <Route path="/admin" element={<AdminLayout />}>
            {adminRoutes.map((route) => (
              <Route
                key={route.path}
                path={route.path}
                element={<route.element />}
              />
            ))}
          </Route>
        </Routes>
      </BrowserRouter>
    </>
  )
}

export default App
