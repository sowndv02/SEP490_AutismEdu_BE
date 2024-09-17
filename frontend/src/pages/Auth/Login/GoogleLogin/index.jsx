import GoogleIcon from '@mui/icons-material/Google';
import { Box, IconButton } from '@mui/material';
import { useGoogleLogin } from '@react-oauth/google';
import Cookies from 'js-cookie';
import services from '~/plugins/services';
function GoogleLogin() {
    const login = useGoogleLogin({
        onSuccess: credentialResponse => {
            services.AuthenticationAPI.loginGoogle({
                token: credentialResponse.code
            }, (res) => {
                Cookies.set('access_token', res.result.accessToken, { expires: 30 })
                Cookies.set('refresh_token', res.result.refreshToken, { expires: 365 })
            }, (err) => {
                console.log(err);
            },
            )
        },
        onError: () => console.log('Login Failed'),
        flow: 'auth-code'
    });

    return (
        <Box sx={{ display: "flex", justifyContent: "center" }}>
            <IconButton onClick={login}>
                <GoogleIcon sx={{ color: "#dd4b39 " }} />
            </IconButton>
        </Box>
    )
}

export default GoogleLogin
