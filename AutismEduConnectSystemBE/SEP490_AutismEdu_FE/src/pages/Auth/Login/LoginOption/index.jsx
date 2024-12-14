import { Box, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import parentLogin from '~/assets/images/parentlogin.png';
import tutorLogin from '~/assets/images/tutorlogin.png';
import PAGES from '~/utils/pages';
function LoginOption() {
    const [isVisible, setIsVisible] = useState(false);
    useEffect(() => {
        setTimeout(() => setIsVisible(true), 100);
    }, []);

    return (
        <Box display="flex" gap={5} justifyContent="center" sx={{ height: "60vh", alignItems: "center" }}>
            <Link to={PAGES.ROOT + PAGES.LOGIN}>
                <Box
                    sx={{
                        width: 300,
                        height: 300,
                        bgcolor: 'primary.main',
                        opacity: isVisible ? 1 : 0,
                        transform: isVisible ? 'translateX(0)' : 'translateX(-50px)',
                        transition: 'opacity 0.6s ease, transform 0.6s ease',
                        cursor: "pointer",
                        textAlign: "center",
                        borderRadius: "10px",
                        '&:hover': {
                            transform: "scale(1.02) translateY(-10px)",
                            boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                        }
                    }}
                >
                    <img src={parentLogin} alt='parentlogin' style={{ maxWidth: "80%", maxHeight: "80%" }} />
                    <Typography color="white">Đăng nhập cho phụ huynh</Typography>
                </Box>
            </Link>
            <Link to={PAGES.TUTOR_LOGIN}>
                <Box
                    sx={{
                        width: 300,
                        height: 300,
                        bgcolor: 'secondary.main',
                        opacity: isVisible ? 1 : 0,
                        transform: isVisible ? 'translateX(0)' : 'translateX(50px)',
                        transition: 'opacity 0.6s ease, transform 0.6s ease',
                        cursor: "pointer",
                        textAlign: "center",
                        borderRadius: "10px",
                        '&:hover': {
                            transform: "scale(1.02) translateY(-10px)",
                            boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                        }
                    }}
                >
                    <img src={tutorLogin} alt='totorlogin' style={{ maxWidth: "80%", height: "80%" }} />
                    <Typography color="white">Đăng nhập cho gia sư</Typography>
                </Box>
            </Link>
        </Box>
    )
}

export default LoginOption
