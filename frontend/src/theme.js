import { createTheme } from '@mui/material/styles';
import { red } from '@mui/material/colors';

// Create a theme instance.
const theme = createTheme({
    myapp: {
        adminHeaderHeight: '70px',
        adminSideBarWidth: '260px'
    },
    palette: {
        primary: {
            main: '#556cd6'
        },
        secondary: {
            main: '#19857b'
        },
        error: {
            main: red.A400
        },
        text: {
            primary: '#676b7b',
            secondary: '#3b4056'
        }
    },
    components: {
        MuiCssBaseline: {
            styleOverrides: `
            a {
              color: #3b4056;
              text-decoration: none;
            }
          `
        }
    }
});

export default theme;