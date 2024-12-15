import RemoveIcon from '@mui/icons-material/Remove';
import { IconButton } from '@mui/material';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import * as React from 'react';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import LockPersonIcon from '@mui/icons-material/LockPerson';
function ConfirmLockDialog({ isLock, name, id, handleChangeUserStatus }) {
    const [open, setOpen] = React.useState(false);

    const handleClickOpen = () => {
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    const handleSubmit = () => {
        handleChangeUserStatus(id, isLock)
        handleClose();
    }
    return (
        <React.Fragment>
            <IconButton onClick={handleClickOpen}>
                {isLock ? <LockOpenIcon /> : <LockPersonIcon />}
            </IconButton>
            <Dialog
                open={open}
                onClose={handleClose}
                aria-labelledby="alert-dialog-title"
                aria-describedby="alert-dialog-description"
            >
                <DialogTitle id="alert-dialog-title">
                    {isLock ? `Bạn có muốn mở khoá tài khoản ${name}` : `Bạn có muốn khoá tài khoản ${name}`}
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleClose} autoFocus variant='outlined'>
                        Huỷ bỏ
                    </Button>
                    <Button onClick={handleSubmit} variant='contained'>
                        {isLock ? "Mở khoá" : "Khoá"}
                    </Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    )
}

export default ConfirmLockDialog
