import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'
import CssBaseline from '@mui/material/CssBaseline'
import { ThemeProvider } from '@mui/material/styles';
import theme from './theme';
import { SnackbarProvider } from 'notistack'
ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <SnackbarProvider anchorOrigin={{
        vertical: 'top',
        horizontal: 'right'
      }}
        autoHideDuration={4000}>
        <App />
      </SnackbarProvider>
    </ThemeProvider>
  </React.StrictMode>
)
