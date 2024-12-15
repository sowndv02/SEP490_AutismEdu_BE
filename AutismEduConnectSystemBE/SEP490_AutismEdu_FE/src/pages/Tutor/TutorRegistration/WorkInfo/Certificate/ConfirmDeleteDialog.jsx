import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import { IconButton } from '@mui/material';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import * as React from 'react';
function ConfirmDeleteDialog({ certificate, setCertificate, index }) {
    const [open, setOpen] = React.useState(false);

    const handleClickOpen = (event) => {
        event.stopPropagation();
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };

    const handleDelete = () => {
        const deleteCertificate = certificate.filter((c, i) => {
            return i !== index;
        })
        setCertificate(deleteCertificate);
        handleClose();
    }
    return (
        <React.Fragment>
            <IconButton onClick={handleClickOpen}>
                <DeleteOutlineIcon />
            </IconButton>
            <Dialog
                open={open}
                onClose={handleClose}
                aria-labelledby="alert-dialog-title"
                aria-describedby="alert-dialog-description"
            >
                <DialogTitle id="alert-dialog-title">
                    Bạn có muốn gỡ chứng chỉ này
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleClose} autoFocus variant='outlined'>
                        Huỷ
                    </Button>
                    <Button onClick={handleDelete} variant='contained' sx={{ ml: 2 }}>Xoá</Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    )
}

export default ConfirmDeleteDialog
