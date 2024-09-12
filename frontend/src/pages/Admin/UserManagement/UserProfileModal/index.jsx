import LockOpenIcon from '@mui/icons-material/LockOpen';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { Avatar, Button, IconButton } from '@mui/material';
import Box from '@mui/material/Box';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import * as React from 'react';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 800,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
    borderRadius: "10px"
};
function UserProfileModal({currentUser}) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    return (
        <div>
            <IconButton onClick={handleOpen}>
                <VisibilityIcon />
            </IconButton>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Box sx={{ display: "flex", gap: "20px" }}>
                        <Box sx={{ width: "30%", textAlign: "center", borderRight: "2px solid gray", pr: "20px" }}>
                            <Avatar alt="Tuan Dao" sx={{ width: 150, height: 150, margin: "auto" }}
                                src="https://backend-api20240823212838.azurewebsites.net/UserImages/default-avatar.png" />
                            <LockOpenIcon sx={{ fontSize: "30px", mt: "10px" }} color='success' />
                            <Typography sx={{ fontSize: "30px" }}>{currentUser.fullName}</Typography>
                        </Box>
                        <Box sx={{ width: "70%" }}>
                            <Typography variant='h4'>Account</Typography>
                            <Box mt={2}>
                                <Typography sx={{ fontWeight: "bold" }}>Email</Typography>
                                <Typography sx={{ borderBottom: "1px solid gray" }}>{currentUser.email}</Typography>
                            </Box>
                            <Box mt={2}>
                                <Typography sx={{ fontWeight: "bold" }}>Phone Number</Typography>
                                <Typography sx={{ borderBottom: "1px solid gray" }}>{currentUser.phoneNumber}</Typography>
                            </Box>
                            <Box mt={2}>
                                <Typography sx={{ fontWeight: "bold" }}>Address</Typography>
                                <Typography sx={{ borderBottom: "1px solid gray" }}>{currentUser.address}</Typography>
                            </Box>
                            <Button variant='outlined' color='inherit' sx={{ width: "50%", mt: "20px" }} onClick={() => setOpen(false)}>Close</Button>
                        </Box>
                    </Box>
                </Box>
            </Modal>
        </div>
    )
}

export default UserProfileModal
