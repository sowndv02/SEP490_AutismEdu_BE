import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, IconButton, TextField } from '@mui/material';
import Paper from '@mui/material/Paper';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import DriveFileRenameOutlineIcon from '@mui/icons-material/DriveFileRenameOutline';
import { useEffect, useState } from 'react';
import DoneIcon from '@mui/icons-material/Done';
import CloseIcon from '@mui/icons-material/Close';
import LoadingComponent from '~/components/LoadingComponent';
import { enqueueSnackbar } from 'notistack';
import services from '~/plugins/services';
function AssessmentUpdater({ open, setOpen, currentAsssesment, assessments, setAssessments }) {
    const [currentEdit, setCurrentEdit] = useState(-1);
    const [updatedValue, setUpdatedValue] = useState("");
    const [assessment, setAssessment] = useState(null);
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        setAssessment(currentAsssesment);
    }, [currentAsssesment])

    useEffect(() => {
        if (!open) {
            setCurrentEdit(-1);
            setUpdatedValue("");
        }
    }, [open])
    const handleChange = (id) => {
        const arr = [...assessment.assessmentOptions];
        const updatedOption = arr.find((a) => {
            return a.id === id
        })
        updatedOption.optionText = updatedValue;
        setAssessment({
            ...assessment,
            assessmentOptions: arr
        })
        setUpdatedValue("");
    }

    const handleUpdate = async () => {
        try {
            setLoading(true);
            await services.AssessmentManagementAPI.updateAssessment({
                id: assessment.id,
                question: assessment.question,
                assessmentOptions: assessment.assessmentOptions
            }, (res) => {
                const updatedAss = assessments.map((a) => {
                    if (a.id === assessment.id)
                        return res.result;
                    else return a;
                })
                setAssessments(updatedAss)
                setOpen(false);
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" })
            })
        } catch (error) {
            enqueueSnackbar("Cập nhật thất bại", { variant: "error" })
        } finally {
            setLoading(false);
        }
    }
    return (
        <>
            <Dialog Dialog
                open={open}
                onClose={() => { setOpen(false) }
                }
                maxWidth={"md"}
            >
                <DialogTitle>Chi tiết đánh giá</DialogTitle>
                <DialogContent>
                    <DialogContentText sx={{ whiteSpace: "break-spaces", wordBreak: 'break-word' }}>
                        Tiêu chí: {currentAsssesment.question}
                    </DialogContentText>
                    <TableContainer component={Paper} sx={{ mt: 2 }}>
                        <Table sx={{ minWidth: 650 }} aria-label="simple table">
                            <TableHead>
                                <TableRow>
                                    <TableCell>Điểm</TableCell>
                                    <TableCell >Nội dung</TableCell>
                                    <TableCell>Hành động</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {assessment && assessment.assessmentOptions.map((row, index) => (
                                    <TableRow
                                        key={row.name}
                                        sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                                    >
                                        <TableCell component="th" scope="row">
                                            {row.point}
                                        </TableCell>
                                        <TableCell align="left">
                                            {
                                                currentEdit === index ? (
                                                    <TextField multiline rows={5} fullWidth
                                                        value={updatedValue}
                                                        onChange={(e) => setUpdatedValue(e.target.value)}
                                                    />
                                                ) : row.optionText
                                            }
                                        </TableCell>
                                        <TableCell>
                                            {
                                                currentEdit !== index ? (
                                                    <IconButton onClick={() => { setCurrentEdit(index); setUpdatedValue(row.optionText) }} color='warning'>
                                                        <DriveFileRenameOutlineIcon />
                                                    </IconButton>
                                                ) :
                                                    <>
                                                        <IconButton onClick={() => { setCurrentEdit(-1); handleChange(row.id) }} color='success'>
                                                            <DoneIcon />
                                                        </IconButton>
                                                        <IconButton onClick={() => { setCurrentEdit(-1) }} color='error'>
                                                            <CloseIcon />
                                                        </IconButton>
                                                    </>
                                            }
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setOpen(false)}>Đóng</Button>
                    <Button onClick={handleUpdate}>Cập nhật</Button>
                </DialogActions>
                <LoadingComponent open={loading} />
            </Dialog >
        </>
    )
}

export default AssessmentUpdater
