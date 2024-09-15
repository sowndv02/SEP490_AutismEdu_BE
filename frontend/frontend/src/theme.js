import { createTheme } from '@mui/material/styles';
import { red } from '@mui/material/colors';
import './assets/css/EuclidCircularARegular.ttf'
// Create a theme instance.
const labelFontSize = "12px";
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
    breakpoints: {
        values: {
            xs: 0,
            sm: 700,
            md: 900,
            lg: 1200,
            xl: 1536,
        },
    },
    typography: {
        fontFamily: 'Euclid Circular, Arial, sans-serif',
        fontSize: 16,
        button: {
            fontFamily: 'Euclid Circular',
            textTransform: 'none', // Loại bỏ in hoa mặc định
        },
        h2: {
            fontSize: {
                xs: "30px",
                md: "44px"
            },
            fontWeight: '700'
        },
        h4: {
            fontSize: "30px",
            fontWeight: '700',
            color: '#192335'
        },
        h5: {
            fontWeight: '700',
            color: '#192335'
        },
        h6: {
            fontSize: "20px",
            fontWeight: '700',
            color: '#192335'
        },
        'h1, h2, h3, h4, h5, h6': {
            fontFamily: 'Euclid Circular',
            color: '#192335'
        },
    },
    components: {
        MuiCssBaseline: {
            styleOverrides: {
                a: {
                    color: '#3b4056',
                    textDecoration: 'none',
                },
                'h1, h2, h3, h4, h5, h6': {
                    fontWeight: 'bold',
                    color: '#192335'
                },
            }
        },
        MuiButton: {
            styleOverrides: {
                root: {
                    textTransform: "none"
                }
            }
        }
    }
});

export default theme;