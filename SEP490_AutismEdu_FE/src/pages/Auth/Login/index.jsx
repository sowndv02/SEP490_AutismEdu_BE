import { useLocation, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import LoginForm from './LoginForm';
import VerifyEmail from '~/components/VerifyEmail';
import PAGES from '~/utils/pages';

function Login() {
  const [verify, setVerify] = useState(false);
  const [emailVerify, setEmailVerify] = useState('');
  const location = useLocation(); 
  const nav = useNavigate();
  const tutorId = location.state?.tutorId;

  const handleLoginSuccess = () => {
    if (tutorId) {
      nav(`${PAGES.ROOT}/tutor-profile/${tutorId}`);
    } else {
      nav(`${PAGES.ROOT}`);
    }
  };

  return (
    <>
      {verify === false ? (
        <LoginForm setVerify={setVerify} setEmailVerify={setEmailVerify} onLoginSuccess={handleLoginSuccess} />
      ) : (
        <VerifyEmail email={emailVerify} setVerify={setVerify} submitState={false} />
      )}
    </>
  );
}

export default Login;
