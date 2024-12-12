import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useNavigate } from 'react-router-dom';
import PAGES from '~/utils/pages';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 600,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
    textAlign: "center"
};

export default function CompleteModal({ hasAccount, open }) {
    const nav = useNavigate();
    return (
        <div>
            <Modal
                open={open}
            >
                <Box sx={style}>
                    <CheckCircleIcon sx={{ fontSize: "120px", color: "#11b823" }} />
                    <Typography variant='h5' textAlign="center">Tạo hồ sơ học sinh thành công</Typography>
                    {
                        hasAccount === "false" && <Typography>Thông tin tài khoản của phụ huynh đã được gửi về email đăng ký</Typography>
                    }
                    {
                        hasAccount === "true" && (
                            <>
                                <Typography>Thông tin hồ sơ học sinh đã được gửi về email của phụ huynh</Typography>
                            </>
                        )
                    }
                    <Button sx={{
                        width: "50%",
                        mt: 4,
                        height: "60px",
                        fontSize: "20px",
                        background: "#11b823",
                        ":hover": {
                            bgcolor: "#00b514"
                        }
                    }} variant='contained' onClick={() => nav(PAGES.MY_STUDENT)}>Đóng</Button>
                </Box>
            </Modal>
        </div>
    );
}
