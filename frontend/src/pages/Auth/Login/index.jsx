import { useState } from 'react';
import LoginForm from './LoginForm';
import VerifyEmail from '~/components/VerifyEmail';

function Login() {
  const [verify, setVerify] = useState(false);
  const [emailVerify, setEmailVerify] = useState("");
  return (
    <>
      {verify === false ? (
        <LoginForm setVerify={setVerify} setEmailVerify={setEmailVerify} />
      ) :
        (
          <VerifyEmail email={emailVerify} setVerify={setVerify} submitState={false} />
        )
      }
    </>
  );
}

export default Login;
