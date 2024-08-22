import { enqueueSnackbar } from 'notistack';
import React, { useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom';
import service from '~/plugins/services'
function ConfirmRegister() {
    const location = useLocation();
    const urlParams = new URLSearchParams(location.search);
    const userId = urlParams.get('userId');
    const code = urlParams.get('code').replaceAll(" ", "+");
    const security = urlParams.get('security').replaceAll(" ", "+");
    const nav = useNavigate();
    useEffect(() => {
        handleSubmit();
    }, [])
    const handleSubmit = async () => {
        console.log({
            code,
            security,
            userId
        });
        try {
            await service.AuthenticationAPI.confirmEmail({
                code,
                security,
                userId
            }, (res) => {
                enqueueSnackbar("Confirm email successfully!", { variant: "success" });
                nav("/login")
            }, (err) => {
                console.log(err);
                if (err.code === 500) {
                    enqueueSnackbar("Failed to confirm your account!", { variant: "error" });
                }
                else enqueueSnackbar(err.error[0], { variant: "error" });
                nav("/login")
            })
        } catch (error) {
            enqueueSnackbar("Failed to confirm your account!", { variant: "error" });
            nav("/login")
        }
    }
    return (
        <></>
    )
}

export default ConfirmRegister
