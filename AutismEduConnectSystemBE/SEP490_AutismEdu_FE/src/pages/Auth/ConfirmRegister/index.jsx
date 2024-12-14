import { enqueueSnackbar } from 'notistack';
import React, { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom';
import service from '~/plugins/services'
import PAGES from '~/utils/pages';
function ConfirmRegister() {
    const location = useLocation();
    const urlParams = new URLSearchParams(location.search);
    const userId = urlParams.get('userId');
    const code = urlParams.get('code').replaceAll(" ", "+");
    const security = urlParams.get('security').replaceAll(" ", "+");
    const nav = useNavigate();
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        setIsSubmitting(true);
    }, [])
    useEffect(() => {
        if (isSubmitting) {
            handleSubmit();
        }
    }, [isSubmitting]);
    const handleSubmit = async () => {
        try {
            await service.AuthenticationAPI.confirmEmail({
                code,
                security,
                userId
            }, (res) => {
                enqueueSnackbar("Xác thực tài khoản thành công!", { variant: "success" });
                nav(PAGES.ROOT + PAGES.LOGIN)
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" });
                nav(PAGES.ROOT + PAGES.LOGIN)
            })
        } catch (error) {
            nav(PAGES.ROOT + PAGES.LOGIN)
        }
    }
    return (
        <></>
    )
}

export default ConfirmRegister
