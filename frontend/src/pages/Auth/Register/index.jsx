import { useState } from 'react';
import VerifyEmail from '~/components/VerifyEmail';
import RegisterForm from './RegisterForm';

function Register() {
    const [verify, setVerify] = useState(false);
    const [emailVerify, setEmailVerify] = useState("");
    return (
        <>
            {verify === false ? (
                <RegisterForm setVerify={setVerify} setEmailVerify={setEmailVerify} />
            ) :
                (
                    <VerifyEmail email={emailVerify} setVerify={setVerify} submitState={true} />
                )
            }
        </>
    );
}

export default Register;
